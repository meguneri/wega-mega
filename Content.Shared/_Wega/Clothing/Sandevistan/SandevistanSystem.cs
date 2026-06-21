using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Implants;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Shared._Wega.Clothing.Sandevistan;

/// <summary>
/// Grants the Sandevistan action while the cyberware is worn, and on activation applies a
/// short speed-boost + damage-reduction burst (<see cref="SandevistanActiveComponent"/>) on
/// a cooldown. Modelled on the adrenaline vest's rush, but action-triggered.
/// </summary>
public sealed partial class SandevistanSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    /// <summary>Low, slowed-down phase tone played to a victim the moment bullet-time grabs them.</summary>
    private static readonly SoundSpecifier SlowedSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg",
        AudioParams.Default.WithPitchScale(0.6f).WithVolume(-4f));

    /// <summary>Looping energetic hum the wearer hears for the whole burst ("you're in the zone").</summary>
    private static readonly SoundSpecifier WearerLoopSound = new SoundPathSpecifier("/Audio/Weapons/ebladehum.ogg",
        AudioParams.Default.WithLoop(true).WithPitchScale(1.25f).WithVolume(-10f));

    /// <summary>Looping deep "time-warp" drone every slowed victim hears while caught in bullet-time.</summary>
    private static readonly SoundSpecifier SlowLoopSound = new SoundPathSpecifier("/Audio/Effects/Grenades/Supermatter/supermatter_loop.ogg",
        AudioParams.Default.WithLoop(true).WithPitchScale(0.7f).WithVolume(-8f));

    /// <summary>Soft "phase out / time resumes" cue when the burst ends or is interrupted.</summary>
    private static readonly SoundSpecifier EndSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg",
        AudioParams.Default.WithPitchScale(0.85f).WithVolume(-6f));

    /// <summary>Chrono-field "time distortion" visual spawned on a mob the instant Projection's touch freezes it.</summary>
    private static readonly EntProtoId ProjectionFreezeEffect = "EffectDesynchronizer";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SandevistanComponent, SandevistanActivateEvent>(OnActivate);

        // Track who currently carries a Sandevistan (any version) so the gloves of the north star can
        // hit harder under all of them, not just the arena lock.
        SubscribeLocalEvent<SandevistanComponent, ClothingGotEquippedEvent>(OnWearerEquipped);
        SubscribeLocalEvent<SandevistanComponent, ClothingGotUnequippedEvent>(OnWearerUnequipped);
        SubscribeLocalEvent<SandevistanComponent, ImplantImplantedEvent>(OnWearerImplanted);
        SubscribeLocalEvent<SandevistanComponent, ImplantRemovedEvent>(OnWearerImplantRemoved);

        SubscribeLocalEvent<SandevistanActiveComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SandevistanActiveComponent, DamageModifyEvent>(OnActiveDamageModify);
        SubscribeLocalEvent<SandevistanSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSlowed);
        SubscribeLocalEvent<SandevistanSlowedComponent, GetMeleeAttackRateEvent>(OnSlowedAttackRate);
        SubscribeLocalEvent<SandevistanSlowedComponent, ShotAttemptedEvent>(OnSlowedShotAttempt);
        SubscribeLocalEvent<SandevistanActiveComponent, ShotAttemptedEvent>(OnActiveShotAttempt);

        // "Projection": the wearer's bare-handed strike ("touch") freezes whoever it lands on.
        SubscribeLocalEvent<SandevistanActiveComponent, MeleeHitEvent>(OnActiveMeleeTouch);
    }

    /// <summary>
    /// Projection Sorcery (Naoya Zenin): while the burst is active, every mob the wearer strikes with
    /// a bare hand is frozen solid — fully stunned in place — for <see cref="SandevistanActiveComponent.TouchFreezeDuration"/>.
    /// Unarmed melee raises <see cref="MeleeHitEvent"/> on the user entity itself (their own fists are
    /// the "weapon"), so this directed handler only fires for the wearer's own touch, never for a held
    /// weapon — fitting a contact technique. Server-authoritative: the stun isn't predicted.
    /// </summary>
    private void OnActiveMeleeTouch(Entity<SandevistanActiveComponent> ent, ref MeleeHitEvent args)
    {
        if (ent.Comp.TouchFreezeDuration <= TimeSpan.Zero || !_net.IsServer)
            return;

        // Don't freeze once the burst has lapsed, even if the component lingers a tick (see OnSlowedShotAttempt).
        if (ent.Comp.EndTime <= _timing.CurTime)
            return;

        foreach (var hit in args.HitEntities)
        {
            if (hit == ent.Owner || !HasComp<MobStateComponent>(hit))
                continue;

            if (!_stun.TryUpdateStunDuration(hit, ent.Comp.TouchFreezeDuration))
                continue;

            _popup.PopupEntity(Loc.GetString("sandevistan-projection-frozen"), hit, hit, PopupType.LargeCaution);
            _audio.PlayPvs(SlowedSound, hit);
            // Chrono-field flash on the frozen target — "time stopped here". Server-spawned; replicates.
            SpawnAttachedTo(ProjectionFreezeEffect, Transform(hit).Coordinates);
        }
    }

    // While the burst is active the wearer is reacting at superhuman speed: roll a chance to fully
    // dodge each incoming attack (the only way to slip instant hitscan shots — those can't be slowed
    // in flight), and otherwise apply the standing incoming-damage reduction.
    private void OnActiveDamageModify(Entity<SandevistanActiveComponent> ent, ref DamageModifyEvent args)
    {
        // Only react to actual incoming attacks from someone, never healing or environmental damage.
        if (args.Origin == null || args.Damage.GetTotal() <= 0)
            return;

        // The dodge roll is random, so decide it on the server and let the negated damage replicate —
        // otherwise the client would flicker the hit.
        if (ent.Comp.DodgeChance > 0f && _net.IsServer && _random.Prob(ent.Comp.DodgeChance))
        {
            args.Damage = new DamageSpecifier();
            _popup.PopupEntity(Loc.GetString("evasion-dodged"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.DamageCoefficient < 1f)
            args.Damage *= ent.Comp.DamageCoefficient;
    }

    // Bullet time drags the trigger as well as the feet: a slowed mob may only fire its gun on the
    // same stretched cadence we slow its movement/melee by. Predicted (runs on both sides).
    private void OnSlowedShotAttempt(Entity<SandevistanSlowedComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ThrottleShot(ref args, ent.Comp.EndTime, ent.Comp.SlowModifier, ref ent.Comp.NextAllowedShot) && _net.IsServer)
            Dirty(ent);
    }

    // The wearer's own ranged fire is stretched by the same factor too: shooting/throwing carry the
    // bullet-time slowdown, so guns are no free advantage — only melee stays at the wearer's full speed.
    private void OnActiveShotAttempt(Entity<SandevistanActiveComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ThrottleShot(ref args, ent.Comp.EndTime, ent.Comp.SlowModifier, ref ent.Comp.NextAllowedShot) && _net.IsServer)
            Dirty(ent);
    }

    /// <summary>
    /// Shared fire-rate throttle: while the burst/slow is live, stretch the gun's fire interval by
    /// <paramref name="slowModifier"/> and gate the next shot. This must run on <em>both</em> client and
    /// server — like every other <see cref="ShotAttemptedEvent"/> handler — so the shooter's predicted gun
    /// stays in lockstep with the server; a server-only cancel would have the client fire shots the server
    /// rejects, draining the predicted magazine and jamming the gun (it then stays broken even after the
    /// burst ends). The gate (<paramref name="nextAllowedShot"/>) is a networked field, so the prediction
    /// reconciles cleanly. Returns true when the gate was advanced (a shot was let through), so the caller
    /// can dirty the networked field on the server.
    /// </summary>
    private bool ThrottleShot(ref ShotAttemptedEvent args, TimeSpan endTime, float slowModifier, ref TimeSpan nextAllowedShot)
    {
        if (args.Cancelled)
            return false;

        var curTime = _timing.CurTime;

        // Slow already lapsed (burst ended; component not yet GC'd, e.g. on a paused arena map between
        // rounds) — fire at full rate. Effects key off EndTime, not mere component existence, so the
        // throttle can never outlive the burst.
        if (endTime <= curTime)
            return false;

        // Still inside the stretched cooldown left by the previous shot.
        if (curTime < nextAllowedShot)
        {
            args.Cancel();
            return false;
        }

        // Stretch the gun's own fire interval by the same factor we slow everything else.
        var gun = args.Used.Comp;
        var baseRate = gun.FireRateModified > 0f ? gun.FireRateModified : gun.FireRate;
        if (baseRate <= 0f || slowModifier <= 0f)
            return false;

        nextAllowedShot = curTime + TimeSpan.FromSeconds(1f / baseRate / slowModifier);
        return true;
    }

    // Caught in bullet time, a mob's swings slow to a crawl as well as its feet — otherwise a wide
    // melee arc would still connect at full speed despite the "frozen" world. We scale the attack
    // rate down by the same factor as movement, so the swing is slow and telegraphed (dodgeable),
    // not blocked outright.
    private void OnSlowedAttackRate(Entity<SandevistanSlowedComponent> ent, ref GetMeleeAttackRateEvent args)
    {
        // Don't slow once the burst has lapsed, even if the component lingers a tick (see OnSlowedShotAttempt).
        if (ent.Comp.EndTime <= _timing.CurTime)
            return;

        args.Multipliers *= ent.Comp.SlowModifier;
    }

    private void OnRefreshSlowed(Entity<SandevistanSlowedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        // Don't re-apply the move slow after the burst lapsed: a stray RefreshMovementSpeedModifiers
        // while the component is still pending removal would otherwise leave the mob crawling.
        if (ent.Comp.EndTime <= _timing.CurTime)
            return;

        args.ModifySpeed(ent.Comp.SlowModifier, ent.Comp.SlowModifier);
    }

    // Standard item-action grant: the action system raises this on the worn/held item to
    // collect the actions it provides. Far more reliable than manually calling AddAction on
    // a clothing-equipped event (which doesn't predict/sync the action bar correctly).
    private void OnGetActions(EntityUid uid, SandevistanComponent comp, GetItemActionsEvent args)
    {
        args.AddAction(ref comp.ActionEntity, comp.Action);
    }

    // ── Wearer marker bookkeeping ────────────────────────────────────────────────────────────────
    // Server-only mutation: the marker is networked and replicates to the client on its own. Adding it
    // client-side (especially during predicted-entity resets) breaks the iteration, same as the arena lock.

    private void OnWearerEquipped(Entity<SandevistanComponent> ent, ref ClothingGotEquippedEvent args)
    {
        AddWearerMarker(args.Wearer);
    }

    private void OnWearerUnequipped(Entity<SandevistanComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemoveWearerMarker(args.Wearer);
    }

    private void OnWearerImplanted(Entity<SandevistanComponent> ent, ref ImplantImplantedEvent args)
    {
        AddWearerMarker(args.Implanted);
    }

    private void OnWearerImplantRemoved(Entity<SandevistanComponent> ent, ref ImplantRemovedEvent args)
    {
        RemoveWearerMarker(args.Implanted);
    }

    /// <summary>Adds one Sandevistan source to <paramref name="mob"/>, attaching the marker on the first.</summary>
    private void AddWearerMarker(EntityUid mob)
    {
        if (!_net.IsServer)
            return;

        var marker = EnsureComp<SandevistanWearerComponent>(mob);
        marker.Sources++;
    }

    /// <summary>Removes one Sandevistan source; the marker lifts only once the last source is gone.</summary>
    private void RemoveWearerMarker(EntityUid mob)
    {
        if (!_net.IsServer || !TryComp<SandevistanWearerComponent>(mob, out var marker))
            return;

        marker.Sources--;
        if (marker.Sources <= 0)
            RemComp<SandevistanWearerComponent>(mob);
    }

    private void OnActivate(Entity<SandevistanComponent> ent, ref SandevistanActivateEvent args)
    {
        var wearer = args.Performer;
        var curTime = _timing.CurTime;

        // Leave args.Handled = false on rejection: marking it handled would make the action system
        // (re)apply the action's useDelay, putting an already-ready button straight back on cooldown
        // ("повторная перезарядка"). We only own the cooldown via NextActivation below.
        if (ent.Comp.NextActivation is { } next && next > curTime)
        {
            _popup.PopupClient(Loc.GetString("sandevistan-recharging"), wearer, wearer);
            return;
        }

        if (HasComp<SandevistanActiveComponent>(wearer))
            return;

        args.Handled = true;
        ent.Comp.NextActivation = curTime + ent.Comp.Cooldown;
        Dirty(ent);

        var active = EnsureComp<SandevistanActiveComponent>(wearer);
        active.EndTime = curTime + ent.Comp.Duration;
        active.SpeedModifier = ent.Comp.SpeedModifier;
        active.DamageCoefficient = ent.Comp.DamageCoefficient;
        active.DodgeChance = ent.Comp.DodgeChance;
        active.SlowRadius = ent.Comp.SlowRadius;
        active.AffectWholeMap = ent.Comp.AffectWholeMap;
        active.SlowModifier = ent.Comp.SlowModifier;
        active.TouchFreezeDuration = ent.Comp.TouchFreezeDuration;
        active.AfterimageInterval = ent.Comp.AfterimageInterval;
        active.AfterimageLifetime = ent.Comp.AfterimageLifetime;
        active.NextAfterimageTime = curTime;
        Dirty(wearer, active);

        _movement.RefreshMovementSpeedModifiers(wearer);
        _audio.PlayPredicted(ent.Comp.ActivationSound, wearer, wearer);
        _popup.PopupClient(Loc.GetString("sandevistan-activated"), wearer, wearer, PopupType.Large);

        // Looping "in the zone" hum for the whole burst. Server-spawned so it persists and can be
        // stopped on expiry; the activation one-shot above stays predicted for snappy feedback.
        if (_net.IsServer)
            active.LoopSound = _audio.PlayPvs(WearerLoopSound, wearer)?.Entity;
    }

    private void OnRefreshSpeed(Entity<SandevistanActiveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedModifier, ent.Comp.SpeedModifier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Server-authoritative: removing the buff isn't predicted.
        if (!_net.IsServer)
            return;

        var curTime = _timing.CurTime;

        // Active users: slow every nearby mob ("bullet time"), and expire the buff itself.
        var query = EntityQueryEnumerator<SandevistanActiveComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            if (active.EndTime <= curTime)
            {
                // Cut the looping hum and snap "time resumes" cue as the burst ends/interrupts.
                _audio.Stop(active.LoopSound);
                _audio.PlayPvs(EndSound, uid);
                RemComp<SandevistanActiveComponent>(uid);
                _movement.RefreshMovementSpeedModifiers(uid);
                continue;
            }

            SlowMobs(uid, active, curTime);
            SlowProjectiles(uid, active, curTime);
            SlowThrownItems(uid, active, curTime, frameTime);
            // Afterimages spawn client-side (SandevistanAfterimageSpawnerSystem) so they appear at
            // the locally-predicted position with no network lag — otherwise they trail far behind.
        }

        // Expire the mob slow once the user has moved away / the burst ended.
        var slowedQuery = EntityQueryEnumerator<SandevistanSlowedComponent>();
        while (slowedQuery.MoveNext(out var uid, out var slowed))
        {
            if (slowed.EndTime > curTime)
                continue;

            _audio.Stop(slowed.LoopSound);
            RemComp<SandevistanSlowedComponent>(uid);
            _movement.RefreshMovementSpeedModifiers(uid);
        }

        // Restore projectiles whose slow window lapsed (burst ended / left influence): divide the
        // velocity back by the exact factor we applied.
        var slowedProjQuery = EntityQueryEnumerator<SandevistanSlowedProjectileComponent>();
        while (slowedProjQuery.MoveNext(out var uid, out var slowedProj))
        {
            if (slowedProj.EndTime > curTime)
                continue;

            if (slowedProj.Factor > 0f && TryComp<PhysicsComponent>(uid, out var body))
            {
                _physics.SetLinearVelocity(uid, body.LinearVelocity / slowedProj.Factor, body: body);
                _physics.SetAngularVelocity(uid, body.AngularVelocity / slowedProj.Factor, body: body);
            }

            RemComp<SandevistanSlowedProjectileComponent>(uid);
        }
    }

    /// <summary>
    /// Slows nearby (or, when <see cref="SandevistanActiveComponent.AffectWholeMap"/>, every) mob —
    /// except the user — to a crawl. A short refreshed window means the slow lifts soon after the
    /// user blurs past / the burst ends.
    /// </summary>
    private void SlowMobs(EntityUid user, SandevistanActiveComponent active, TimeSpan curTime)
    {
        var window = curTime + TimeSpan.FromSeconds(0.6);

        if (active.AffectWholeMap)
        {
            var userMap = Transform(user).MapID;
            var query = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
            while (query.MoveNext(out var mob, out _, out var xform))
            {
                if (mob == user || xform.MapID != userMap)
                    continue;

                ApplyMobSlow(mob, active, window);
            }

            return;
        }

        var targets = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(Transform(user).Coordinates, active.SlowRadius, targets);
        foreach (var target in targets)
        {
            if (target.Owner == user)
                continue;

            ApplyMobSlow(target.Owner, active, window);
        }
    }

    private void ApplyMobSlow(EntityUid mob, SandevistanActiveComponent active, TimeSpan window)
    {
        var isNew = !HasComp<SandevistanSlowedComponent>(mob);
        var slowed = EnsureComp<SandevistanSlowedComponent>(mob);
        var changed = isNew || slowed.SlowModifier != active.SlowModifier;
        slowed.SlowModifier = active.SlowModifier;
        slowed.EndTime = window;
        Dirty(mob, slowed);

        // Only recompute speed when first applied (or modifier changed) — refreshing every tick
        // would thrash the movement system.
        if (changed)
            _movement.RefreshMovementSpeedModifiers(mob);

        // First time the victim is caught this burst: tell them why the world just crawled. The
        // fullscreen "bullet time" overlay is driven client-side by SandevistanSlowedComponent;
        // here we add the in-fiction feedback (popup + a low phase-out tone). Server-only so it
        // fires once per entry, not every refresh tick.
        if (isNew && _net.IsServer)
        {
            _popup.PopupEntity(Loc.GetString("sandevistan-slowed-victim"), mob, mob, PopupType.MediumCaution);
            _audio.PlayEntity(SlowedSound, mob, mob);
            // Looping "time-warp" drone for as long as the victim stays slowed; stopped when the slow lifts.
            slowed.LoopSound = _audio.PlayPvs(SlowLoopSound, mob)?.Entity;
        }
    }

    /// <summary>
    /// Scales down the velocity of every projectile on the user's map (bullet time), except those
    /// the user fired. Each projectile is scaled once (guarded by the marker component) and its
    /// window is refreshed each tick; restoration happens in <see cref="Update"/> when the window
    /// lapses. When not <see cref="SandevistanActiveComponent.AffectWholeMap"/>, only projectiles
    /// within <see cref="SandevistanActiveComponent.SlowRadius"/> are affected.
    /// </summary>
    private void SlowProjectiles(EntityUid user, SandevistanActiveComponent active, TimeSpan curTime)
    {
        var window = curTime + TimeSpan.FromSeconds(0.6);
        var userMap = Transform(user).MapID;
        var userCoords = Transform(user).Coordinates;

        var query = EntityQueryEnumerator<ProjectileComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var proj, out var body, out var xform))
        {
            // The wearer's own shots are slowed too — bullet-time applies the same drag to their fire.
            if (xform.MapID != userMap)
                continue;

            if (!active.AffectWholeMap
                && (Transform(uid).Coordinates.TryDistance(EntityManager, _transform, userCoords, out var dist) ? dist : float.MaxValue) > active.SlowRadius)
                continue;

            // Already slowed by an active burst — just refresh its window.
            if (TryComp<SandevistanSlowedProjectileComponent>(uid, out var existing))
            {
                existing.EndTime = window;
                continue;
            }

            var slowedProj = AddComp<SandevistanSlowedProjectileComponent>(uid);
            slowedProj.Factor = active.SlowModifier;
            slowedProj.EndTime = window;

            _physics.SetLinearVelocity(uid, body.LinearVelocity * active.SlowModifier, body: body);
            _physics.SetAngularVelocity(uid, body.AngularVelocity * active.SlowModifier, body: body);
        }
    }

    /// <summary>
    /// Same bullet-time treatment as <see cref="SlowProjectiles"/>, but for thrown items (which are
    /// not projectiles): their flight velocity is scaled down and their landing timer is stretched so
    /// they actually crawl through the air instead of dropping early. The user's own throws keep full
    /// speed, just like their own shots. Restored by the shared marker cleanup in <see cref="Update"/>.
    /// </summary>
    private void SlowThrownItems(EntityUid user, SandevistanActiveComponent active, TimeSpan curTime, float frameTime)
    {
        if (active.SlowModifier <= 0f)
            return;

        var window = curTime + TimeSpan.FromSeconds(0.6);
        var userMap = Transform(user).MapID;
        var userCoords = Transform(user).Coordinates;

        var query = EntityQueryEnumerator<ThrownItemComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var thrown, out var body, out var xform))
        {
            // The wearer's own thrown items are slowed too — same bullet-time drag as their shots.
            if (xform.MapID != userMap)
                continue;

            if (!active.AffectWholeMap
                && (xform.Coordinates.TryDistance(EntityManager, _transform, userCoords, out var dist) ? dist : float.MaxValue) > active.SlowRadius)
                continue;

            // Stretch the landing countdown so the slowed item stays airborne proportionally longer
            // (only the slowed fraction of real time counts toward landing).
            if (thrown.LandTime is { } landTime)
            {
                thrown.LandTime = landTime + TimeSpan.FromSeconds(frameTime * (1f - active.SlowModifier));
                Dirty(uid, thrown);
            }

            // Already slowed by an active burst — just refresh its window.
            if (TryComp<SandevistanSlowedProjectileComponent>(uid, out var existing))
            {
                existing.EndTime = window;
                continue;
            }

            var slowed = AddComp<SandevistanSlowedProjectileComponent>(uid);
            slowed.Factor = active.SlowModifier;
            slowed.EndTime = window;

            _physics.SetLinearVelocity(uid, body.LinearVelocity * active.SlowModifier, body: body);
            _physics.SetAngularVelocity(uid, body.AngularVelocity * active.SlowModifier, body: body);
        }
    }
}
