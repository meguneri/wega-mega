using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Weapons.RandomMagazine;

/// <summary>
/// On MapInit, picks a random magazine from <see cref="Magazines"/> and inserts it into the "gun_magazine" slot.
/// Replaces whatever startingItem was set on that slot.
/// </summary>
[RegisterComponent]
public sealed partial class RandomMagazineComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Magazines = new();
}
