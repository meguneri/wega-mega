using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Sponsors;

/// <summary>
/// Defines a sponsor tier with associated perks such as priority join,
/// extra character slots, and a custom OOC chat color.
/// </summary>
[Prototype]
public sealed partial class SponsorTierPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Display name of the sponsor tier.
    /// </summary>
    [DataField("name")]
    public string Name = string.Empty;

    /// <summary>
    /// Whether this tier grants priority queue join.
    /// </summary>
    [DataField("priorityJoin")]
    public bool PriorityJoin = false;

    /// <summary>
    /// Number of extra character slots granted by this tier.
    /// </summary>
    [DataField("extraCharSlots")]
    public int ExtraCharSlots = 0;

    /// <summary>
    /// OOC chat color for this sponsor tier (HTML hex format, e.g. "#A0D2EB").
    /// </summary>
    [DataField("oocColor")]
    public Color OocColor = Color.White;
}
