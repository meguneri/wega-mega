using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Wega.Ghost;

/// <summary>
/// Defines a cosmetic ghost appearance theme available to sponsors.
/// </summary>
[Prototype]
public sealed partial class GhostThemePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Display name shown in the UI.
    /// </summary>
    [DataField("name")]
    public string Name = string.Empty;

    /// <summary>
    /// Icon shown in the ghost theme selection UI.
    /// </summary>
    [DataField("icon")]
    public SpriteSpecifier? Icon;
}
