using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Toggleable;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Wega.Clothing.AdaptiveArmor;

/// <summary>
/// Drives the adaptive plating vest: while worn, the armour records every damage type of each incoming
/// attack and, on later hits, mitigates each type it has already seen by
/// <see cref="AdaptiveArmorActiveComponent.AdaptCoefficient"/> for
/// <see cref="AdaptiveArmorActiveComponent.AdaptDuration"/>. The first hit of any new type always lands in
/// full, so an attacker must keep introducing fresh damage types to stay effective — and a multi-type
/// weapon (energy sword, mixed shotgun shot) only gets one free volley before <em>all</em> of its types are
/// hardened against, instead of leaving its secondary type permanently unblocked.
/// </summary>
public sealed partial class AdaptiveArmorSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    /// <summary>Heavy mechanical "the wheel turns one notch" clunk-click, played the instant a blow is
    /// actually absorbed — the Mahoraga shrine-wheel ratcheting round. Deep, loud revolver cylinder for an
    /// unmistakable, weighty click; no new audio assets needed.</summary>
    private static readonly SoundSpecifier AdaptSound =
        new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/revolver_cock.ogg",
            AudioParams.Default.WithPitchScale(0.6f).WithVolume(12f));

    /// <summary>Lighter, higher "the plating retunes" click, played when the armour first tastes a new damage
    /// type (the blow lands in full, but the wheel learns it for next time). Quieter and brighter than
    /// <see cref="AdaptSound"/> so the two events are clearly distinct by ear.</summary>
    private static readonly SoundSpecifier AdaptLearnSound =
        new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/revolver_cock.ogg",
            AudioParams.Default.WithPitchScale(1.25f).WithVolume(4f));

    /// <summary>Persistent spinning Dharmachakra that hovers above the wearer's head while the armour is worn.</summary>
    private static readonly EntProtoId AdaptWheelEffect = "EffectMahoragaWheel";

    /// <summary>One-shot expanding ring spat out at the moment a blow is absorbed.</summary>
    private static readonly EntProtoId AdaptShockwave = "EffectAdaptiveShockwave";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdaptiveArmorComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<AdaptiveArmorComponent, ClothingGotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<AdaptiveArmorActiveComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnEquipped(Entity<AdaptiveArmorComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.Wearer = args.Wearer;
        Dirty(ent);

        var active = EnsureComp<AdaptiveArmorActiveComponent>(args.Wearer);
        active.AdaptDuration = ent.Comp.AdaptDuration;
        active.AdaptCoefficient = ent.Comp.AdaptCoefficient;
        active.Vest = ent.Owner;

        // Start with the accent dark; it lights up on the first adaptation.
        SetGlow(ent.Owner, false, AdaptiveArmorColors.Default);

        // Hang the persistent wheel above the wearer's head, parented directly to the wearer so it
        // follows them around on its own; we only remember it so we can delete it on unequip.
        // Server-authoritative: the spawned entity replicates to every client.
        if (_net.IsServer)
            active.WheelEffect = Spawn(AdaptWheelEffect, new EntityCoordinates(args.Wearer, default));

        Dirty(args.Wearer, active);
    }

    private void OnUnequipped(Entity<AdaptiveArmorComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        ent.Comp.Wearer = null;
        Dirty(ent);

        SetGlow(ent.Owner, false, AdaptiveArmorColors.Default);

        if (_net.IsServer && TryComp<AdaptiveArmorActiveComponent>(args.Wearer, out var active) && active.WheelEffect is { } wheel)
            QueueDel(wheel);

        RemComp<AdaptiveArmorActiveComponent>(args.Wearer);
    }

    /// <summary>Prune adaptations as their individual windows pass; once the last one lapses with no fresh
    /// hit, darken the accent, reset the wheel tint and empty the segment gauge. Server-authoritative.</summary>
    public override void Update(float frameTime)
    {
        if (!_net.IsServer)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<AdaptiveArmorActiveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.GlowActive)
                continue;

            // Drop every adaptation whose own window has elapsed.
            var changed = false;
            foreach (var type in comp.AdaptedTypes.Keys.ToArray())
            {
                if (comp.AdaptedTypes[type] <= now)
                {
                    comp.AdaptedTypes.Remove(type);
                    changed = true;
                }
            }

            // Some adaptations still hold — keep the glow lit.
            if (comp.AdaptedTypes.Count > 0)
            {
                if (changed)
                    Dirty(uid, comp);
                continue;
            }

            comp.GlowActive = false;
            Dirty(uid, comp);

            SetGlow(comp.Vest, false, AdaptiveArmorColors.Default);
            SetWheelTint(comp, null);
        }
    }

    private void OnDamageModify(Entity<AdaptiveArmorActiveComponent> ent, ref DamageModifyEvent args)
    {
        // Only react to real incoming attacks, never healing or environmental zeroes.
        if (args.Origin == null || args.Damage.GetTotal() <= 0)
            return;

        var comp = ent.Comp;
        var curTime = _timing.CurTime;

        // Snapshot the attack as dealt, before our own mitigation shrinks anything: the full set of damage
        // types in this hit, plus the dominant one (drives the wheel tint / shockwave colour). Capturing it
        // first means a multi-type weapon can't dodge adaptation by having its secondary type quietly slip
        // under the mitigated dominant.
        var incoming = new List<string>();
        string? dominant = null;
        var dominantValue = FixedPoint2.Zero;
        foreach (var (type, amount) in args.Damage.DamageDict)
        {
            if (amount <= FixedPoint2.Zero)
                continue;

            incoming.Add(type);
            if (amount > dominantValue)
            {
                dominantValue = amount;
                dominant = type;
            }
        }

        if (dominant == null)
            return;

        // Mitigate every type in this hit the armour is already adapted to (and still within its window).
        var mitigated = false;
        foreach (var type in incoming)
        {
            if (comp.AdaptedTypes.TryGetValue(type, out var expiry)
                && expiry > curTime
                && args.Damage.DamageDict.TryGetValue(type, out var amount)
                && amount > FixedPoint2.Zero)
            {
                args.Damage.DamageDict[type] = amount * comp.AdaptCoefficient;
                mitigated = true;
            }
        }

        // Learn from this hit. State change is server-authoritative; the reduction above runs on both so the
        // predicted damage matches.
        if (!_net.IsServer)
            return;

        // Adapt to (or refresh) every type the hit carried; flag whether any of them is genuinely new so the
        // "just adapted" feedback only fires the first time a type shows up.
        var learnedNew = false;
        foreach (var type in incoming)
        {
            if (!comp.AdaptedTypes.TryGetValue(type, out var expiry) || expiry <= curTime)
                learnedNew = true;

            comp.AdaptedTypes[type] = curTime + comp.AdaptDuration;
        }

        comp.GlowActive = true;
        Dirty(ent);

        var color = AdaptiveArmorColors.ForType(dominant);
        SetGlow(comp.Vest, true, color);
        SetWheelTint(comp, dominant);

        if (mitigated)
        {
            // The wheel turned in time: at least one type in this blow was absorbed. Ratchet it round one
            // notch with a weighty click, throw a shockwave and tell the wearer their armour just ate the hit.
            _popup.PopupEntity(Loc.GetString("adaptive-armor-absorbed"), ent.Owner, ent.Owner, PopupType.Medium);
            _audio.PlayPvs(AdaptSound, ent.Owner);
            SpinWheel(comp, true);
            SpawnShockwave(ent.Owner, dominant);
        }
        else if (learnedNew)
        {
            // First taste of a fresh threat — landed in full. The plating retunes for next time, with a
            // lighter click so the wearer hears the armour adapt even though this blow got through.
            _popup.PopupEntity(Loc.GetString("adaptive-armor-adapted"), ent.Owner, ent.Owner, PopupType.SmallCaution);
            _audio.PlayPvs(AdaptLearnSound, ent.Owner);
            SpinWheel(comp, false);
        }
    }

    /// <summary>Light or darken the vest's emissive accent and set its tint, via the shared ToggleableVisuals
    /// appearance the client reads to colour the worn clothing layer.</summary>
    private void SetGlow(EntityUid? vest, bool enabled, Color color)
    {
        if (vest is not { } uid)
            return;

        _appearance.SetData(uid, ToggleableVisuals.Enabled, enabled);
        _appearance.SetData(uid, ToggleableVisuals.Color, color);
    }

    /// <summary>Tint the wheel's glow by the live adapted type (empty = idle gold).</summary>
    private void SetWheelTint(AdaptiveArmorActiveComponent comp, string? type)
    {
        if (comp.WheelEffect is { } wheel)
            _appearance.SetData(wheel, AdaptiveWheelVisuals.Type, type ?? string.Empty);
    }

    /// <summary>Ratchet the wheel one notch: bump the spin counter so the client advances the spokes (and
    /// their rings) 45° with a click. <paramref name="strong"/> marks an actual absorb (a longer, brighter
    /// pop) versus merely learning a new type.</summary>
    private void SpinWheel(AdaptiveArmorActiveComponent comp, bool strong)
    {
        if (comp.WheelEffect is not { } wheel)
            return;

        comp.WheelSpin++;
        _appearance.SetData(wheel, AdaptiveWheelVisuals.Strong, strong);
        _appearance.SetData(wheel, AdaptiveWheelVisuals.Spin, comp.WheelSpin);
    }

    /// <summary>Spit out a one-shot expanding ring tinted by the absorbed damage type.</summary>
    private void SpawnShockwave(EntityUid wearer, string type)
    {
        var shock = Spawn(AdaptShockwave, new EntityCoordinates(wearer, default));
        _appearance.SetData(shock, AdaptiveWheelVisuals.Type, type);
    }
}
