using Content.Shared._Wega.Aphrodisiac;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Wega.Aphrodisiac;

/// <summary>
///     Включает/выключает оверлей любовного опьянения, когда на локальном игроке
///     появляется или пропадает статус-эффект с <see cref="LoveVisionStatusEffectComponent"/>.
///     Зеркало DrugOverlaySystem (радужное зрение).
/// </summary>
public sealed partial class LoveVisionOverlaySystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;

    private LoveVisionOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoveVisionStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<LoveVisionStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);

        SubscribeLocalEvent<LoveVisionStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnPlayerAttached);
        SubscribeLocalEvent<LoveVisionStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnPlayerDetached);

        _overlay = new();
    }

    private void OnApplied(Entity<LoveVisionStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnRemoved(Entity<LoveVisionStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        _overlay.Reset();
        _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(Entity<LoveVisionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<LoveVisionStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        _overlay.Reset();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
