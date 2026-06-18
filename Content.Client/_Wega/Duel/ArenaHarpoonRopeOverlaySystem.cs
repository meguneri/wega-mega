using Robust.Client.Graphics;

namespace Content.Client._Wega.Duel;

/// <summary>
/// Держит <see cref="ArenaHarpoonRopeOverlay"/> зарегистрированным. Оверлей сам перебирает сущности
/// с <c>ArenaHarpoonRopeComponent</c> на карте глаза игрока, поэтому системе достаточно его добавить.
/// </summary>
public sealed partial class ArenaHarpoonRopeOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    private ArenaHarpoonRopeOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new();
        _overlayMan.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay(_overlay);
    }
}
