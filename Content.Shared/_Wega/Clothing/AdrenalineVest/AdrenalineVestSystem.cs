using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Wega.Clothing.AdrenalineVest;

/// <summary>
/// Watches the wearer's health and triggers the vest's adrenaline rush
/// (one-time heal + temporary speed boost and damage resistance) when it
/// drops below the configured fraction.
/// </summary>
public sealed partial class AdrenalineVestSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobThresholdSystem _thresholds = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdrenalineVestComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<AdrenalineVestComponent, ClothingGotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<AdrenalineRushComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<AdrenalineRushComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnEquipped(Entity<AdrenalineVestComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.Wearer = args.Wearer;
        Dirty(ent);
    }

    private void OnUnequipped(Entity<AdrenalineVestComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.Wearer = null;
        Dirty(ent);
    }

    private void OnRefreshSpeed(Entity<AdrenalineRushComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.SpeedModifier, ent.Comp.SpeedModifier);
    }

    private void OnDamageModify(Entity<AdrenalineRushComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage *= ent.Comp.DamageCoefficient;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Server-authoritative: healing and component add/remove are not predicted.
        if (!_net.IsServer)
            return;

        var curTime = _timing.CurTime;

        var rushQuery = EntityQueryEnumerator<AdrenalineRushComponent>();
        while (rushQuery.MoveNext(out var uid, out var rush))
        {
            if (rush.EndTime > curTime)
                continue;

            RemComp<AdrenalineRushComponent>(uid);
            _movement.RefreshMovementSpeedModifiers(uid);
        }

        var vestQuery = EntityQueryEnumerator<AdrenalineVestComponent>();
        while (vestQuery.MoveNext(out var uid, out var vest))
        {
            if (vest.Wearer is not { } wearer || HasComp<AdrenalineRushComponent>(wearer))
                continue;

            if (vest.NextActivation != null && vest.NextActivation > curTime)
                continue;

            if (!TryComp<DamageableComponent>(wearer, out var damageable)
                || !_thresholds.TryGetThresholdForState(wearer, MobState.Critical, out var critThreshold))
            {
                continue;
            }

            var healthFraction = 1f - (_damageable.GetTotalDamage((wearer, damageable)) / critThreshold.Value).Float();
            if (healthFraction > vest.HealthFraction || healthFraction <= 0f)
                continue;

            Trigger((uid, vest), wearer, curTime);
        }
    }

    private void Trigger(Entity<AdrenalineVestComponent> vest, EntityUid wearer, TimeSpan curTime)
    {
        vest.Comp.NextActivation = curTime + vest.Comp.Cooldown;
        Dirty(vest);

        if (!vest.Comp.Healing.Empty)
            _damageable.TryChangeDamage(wearer, vest.Comp.Healing, ignoreResistances: true);

        var rush = EnsureComp<AdrenalineRushComponent>(wearer);
        rush.EndTime = curTime + vest.Comp.RushDuration;
        rush.SpeedModifier = vest.Comp.SpeedModifier;
        rush.DamageCoefficient = vest.Comp.DamageCoefficient;
        Dirty(wearer, rush);

        _movement.RefreshMovementSpeedModifiers(wearer);
        _audio.PlayPvs(vest.Comp.ActivationSound, wearer);
        _popup.PopupEntity(Loc.GetString("adrenaline-vest-triggered"), wearer, wearer, PopupType.LargeCaution);
    }
}
