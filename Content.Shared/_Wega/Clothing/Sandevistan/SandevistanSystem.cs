using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<SandevistanComponent, SandevistanActivateEvent>(OnActivate);

        SubscribeLocalEvent<SandevistanActiveComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<SandevistanActiveComponent, DamageModifyEvent>(OnActiveDamageModify);
        SubscribeLocalEvent<SandevistanSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSlowed);
        SubscribeLocalEvent<SandevistanSlowedComponent, GetMeleeAttackRateEvent>(OnSlowedAttackRate);
        SubscribeLocalEvent<SandevistanSlowedComponent, ShotAttemptedEvent>(OnSlowedShotAttempt);
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
    // same stretched cadence we slow its movement/melee by. Server-authoritative throttle.
    private void OnSlowedShotAttempt(Entity<SandevistanSlowedComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!_net.IsServer || args.Cancelled)
            return;

        var curTime = _timing.CurTime;

        // Slow already lapsed (burst ended; component not yet GC'd, e.g. on a paused arena map between
        // rounds) — fire at full rate. Effects key off EndTime, not mere component existence, so the
        // throttle can never outlive the burst.
        if (ent.Comp.EndTime <= curTime)
            return;

        // Still inside the stretched cooldown left by the previous shot.
        if (curTime < ent.Comp.NextAllowedShot)
        {
            args.Cancel();
            return;
        }

        // Stretch the gun's own fire interval by the same factor we slow everything else.
        var gun = args.Used.Comp;
        var baseRate = gun.FireRateModified > 0f ? gun.FireRateModified : gun.FireRate;
        if (baseRate <= 0f || ent.Comp.SlowModifier <= 0f)
            return;

        var slowedInterval = TimeSpan.FromSeconds(1f / baseRate / ent.Comp.SlowModifier);
        ent.Comp.NextAllowedShot = curTime + slowedInterval;
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

    private void OnActivate(Entity<SandevistanComponent> ent, ref SandevistanActivateEvent args)
    {
        args.Handled = true;

        var wearer = args.Performer;
        var curTime = _timing.CurTime;

        if (ent.Comp.NextActivation is { } next && next > curTime)
        {
            _popup.PopupClient(Loc.GetString("sandevistan-recharging"), wearer, wearer);
            return;
        }

        if (HasComp<SandevistanActiveComponent>(wearer))
            return;

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
        active.AfterimageInterval = ent.Comp.AfterimageInterval;
        active.AfterimageLifetime = ent.Comp.AfterimageLifetime;
        active.NextAfterimageTime = curTime;
        Dirty(wearer, active);

        _movement.RefreshMovementSpeedModifiers(wearer);
        _audio.PlayPredicted(ent.Comp.ActivationSound, wearer, wearer);
        _popup.PopupClient(Loc.GetString("sandevistan-activated"), wearer, wearer, PopupType.Large);
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
                RemComp<SandevistanActiveComponent>(uid);
                _movement.RefreshMovementSpeedModifiers(uid);
                continue;
            }

            SlowMobs(uid, active, curTime);
            SlowProjectiles(uid, active, curTime);
            SlowThrownItems(uid, active, curTime, frameTime);
            SpawnAfterimages((uid, active), curTime);
        }

        // Expire the mob slow once the user has moved away / the burst ended.
        var slowedQuery = EntityQueryEnumerator<SandevistanSlowedComponent>();
        while (slowedQuery.MoveNext(out var uid, out var slowed))
        {
            if (slowed.EndTime > curTime)
                continue;

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
            // The user's own shots keep full speed — only the world around them slows.
            if (proj.Shooter == user)
                continue;

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
            // The user's own thrown items keep full speed — only the world around them slows.
            if (thrown.Thrower == user)
                continue;

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

    /// <summary>
    /// Leaves a trail of translucent blue "ghosts" behind the moving user (David Martinez
    /// style). Bare entities are spawned at the user's spot; the client copies the user's sprite
    /// onto them, and they fade out via <see cref="TimedDespawnComponent"/>.
    /// </summary>
    private void SpawnAfterimages(Entity<SandevistanActiveComponent> ent, TimeSpan curTime)
    {
        if (curTime < ent.Comp.NextAfterimageTime)
            return;

        ent.Comp.NextAfterimageTime = curTime + ent.Comp.AfterimageInterval;

        var xform = Transform(ent.Owner);
        var afterimage = Spawn(null, xform.Coordinates);

        var comp = EnsureComp<SandevistanAfterimageComponent>(afterimage);
        comp.SourceEntity = ent.Owner;
        comp.DirectionOverride = xform.LocalRotation.GetCardinalDir();
        Dirty(afterimage, comp);

        var despawn = EnsureComp<TimedDespawnComponent>(afterimage);
        despawn.Lifetime = (float) ent.Comp.AfterimageLifetime.TotalSeconds;
    }
}
