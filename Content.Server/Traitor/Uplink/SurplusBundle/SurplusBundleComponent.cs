namespace Content.Server.Traitor.Uplink.SurplusBundle;

/// <summary>
///     Fill crate with a random uplink items.
/// </summary>
[RegisterComponent]
public sealed partial class SurplusBundleComponent : Component
{
    /// <summary>
    ///     Total price of all content inside bundle.
    /// </summary>
    [DataField]
    public int TotalPrice = 20;

    /// <summary>
    ///     Maximum number of items allowed per store category (keyed by category ID string).
    ///     Items with any category that hits its limit are excluded from further picks.
    /// </summary>
    [DataField]
    public Dictionary<string, int> CategoryLimits = new();
}
