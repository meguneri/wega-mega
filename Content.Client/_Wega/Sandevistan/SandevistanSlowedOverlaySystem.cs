using Robust.Client.Graphics;

namespace Content.Client._Wega.Sandevistan;

/// <summary>
/// Registers the Sandevistan fullscreen overlays: the victims' "slowed" look and the wearer's
/// "charged" look. Each overlay self-gates on the local player's component and eases its own
/// intensity, so the system just keeps them registered.
/// </summary>
public sealed partial class SandevistanSlowedOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private SandevistanSlowedOverlay _slowedOverlay = default!;
    private SandevistanActiveOverlay _activeOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _slowedOverlay = new();
        _activeOverlay = new();
        _overlayMan.AddOverlay(_slowedOverlay);
        _overlayMan.AddOverlay(_activeOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayMan.RemoveOverlay(_slowedOverlay);
        _overlayMan.RemoveOverlay(_activeOverlay);
    }
}
