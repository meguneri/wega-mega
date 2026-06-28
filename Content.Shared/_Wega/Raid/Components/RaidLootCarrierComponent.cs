using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Raid.Components;

[RegisterComponent]
public sealed partial class RaidLootCarrierComponent : Component
{
    /// <summary>Гарантированный дроп при смерти (для боссов). Спавнится у трупа и метится добычей.</summary>
    [DataField]
    public List<EntProtoId> BonusLoot = new();
}
