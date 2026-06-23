using Content.Shared._Wega.Magic.FreezeSpell;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Magic.FreezeSpell;

public sealed partial class FreezeSpellSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly TimeSpan FreezeDuration = TimeSpan.FromSeconds(6);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FreezeSpellEvent>(OnFreeze);
        SubscribeLocalEvent<FrozenInIceComponent, ComponentShutdown>(OnFreezeEnd);
    }

    private void OnFreeze(FreezeSpellEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        if (!HasComp<MobStateComponent>(target))
            return;

        args.Handled = true;

        _stun.TryAddParalyzeDuration(target, FreezeDuration);
        EnsureComp<FrozenInIceComponent>(target);

        Timer.Spawn(FreezeDuration, () =>
        {
            if (Exists(target))
                RemCompDeferred<FrozenInIceComponent>(target);
        });

        _popup.PopupEntity(
            Loc.GetString("freeze-spell-target"),
            target, target, PopupType.LargeCaution);
        _popup.PopupEntity(
            Loc.GetString("freeze-spell-caster", ("target", Name(target))),
            args.Performer, args.Performer, PopupType.Medium);
    }

    private void OnFreezeEnd(EntityUid uid, FrozenInIceComponent comp, ComponentShutdown args)
    {
        _status.TryRemoveStatusEffect(uid, SharedStunSystem.StunId);
    }
}
