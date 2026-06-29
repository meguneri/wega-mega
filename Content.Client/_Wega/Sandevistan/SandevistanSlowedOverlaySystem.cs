using Robust.Client.Graphics;

namespace Content.Client._Wega.Sandevistan;

/// <summary>
/// Registers the Sandevistan fullscreen overlays: the victims' "slowed" look, the wearer's "charged"
/// speed-vision, and the wearer's cyberpunk "glitch" interference. Each overlay self-gates on the local
/// player's component and eases its own intensity, so the system just keeps them registered.
/// </summary>
public sealed partial class SandevistanSlowedOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private SandevistanSlowedOverlay _slowedOverlay = default!;
    private SandevistanActiveOverlay _activeOverlay = default!;
    private SandevistanGlitchOverlay _glitchOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _slowedOverlay = new();
        _activeOverlay = new();
        _glitchOverlay = new();
        _overlayMan.AddOverlay(_slowedOverlay);
        _overlayMan.AddOverlay(_activeOverlay);
        _overlayMan.AddOverlay(_glitchOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMan.RemoveOverlay(_slowedOverlay);
        _overlayMan.RemoveOverlay(_activeOverlay);
        _overlayMan.RemoveOverlay(_glitchOverlay);
    }
}
