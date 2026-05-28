using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Duel.Components;

[RegisterComponent]
public sealed partial class DuelArenaComponent : Component
{
    [DataField]
    public float ScanRange = 10f;

    public readonly HashSet<EntityUid> Duelists = new();
    public bool IsActive;
}
