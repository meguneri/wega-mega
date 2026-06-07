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
    ///     Hard cap on the number of items generated, regardless of remaining budget.
    ///     Keeps the bundle from overflowing the crate's storage capacity (default 30):
    ///     extra items would otherwise fail to insert and spill onto the floor.
    /// </summary>
    [DataField]
    public int MaxItems = 30;

    /// <summary>
    ///     Maximum number of items allowed per store category (keyed by category ID string).
    ///     Items with any category that hits its limit are excluded from further picks.
    /// </summary>
    [DataField]
    public Dictionary<string, int> CategoryLimits = new();

    /// <summary>
    ///     Listing IDs that are never rolled into this bundle. Lets a single shared pool exclude
    ///     specific items from one crate (e.g. drop grenade launchers from the big chaos box only).
    /// </summary>
    [DataField]
    public HashSet<string> ExcludedListings = new();

    /// <summary>
    ///     Categories from which at least one item is guaranteed to be picked before the
    ///     regular random fill, as long as the budget allows. Used, for example, to ensure
    ///     a melee arena crate always contains at least one melee weapon.
    /// </summary>
    [DataField]
    public List<string> GuaranteedCategories = new();

    /// <summary>
    ///     If true, item picks are weighted by cost (probability ∝ price^<see cref="CostWeightExponent"/>)
    ///     instead of uniform. This biases the bundle toward more expensive gear, so each opening yields a
    ///     capable loadout rather than a pile of cheap filler — used by the duel arena crates.
    ///     Vanilla surplus crates leave this false (uniform selection).
    /// </summary>
    [DataField]
    public bool WeightByCost;

    /// <summary>
    ///     Exponent applied to item price when <see cref="WeightByCost"/> is set: weight = price^exponent.
    ///     1.0 = linear (price-proportional, strongly favours the priciest items); 0.5 = square root
    ///     (gentle bias, keeps top-tier items a rare jackpot); 0 = uniform. Lower it for rarer big-ticket drops.
    /// </summary>
    [DataField]
    public double CostWeightExponent = 0.5;

    /// <summary>
    ///     If true, every item generated into this bundle is tagged with
    ///     <c>ArenaIssuedItemComponent</c>. Used by the duel arena crates so the arena cleanup
    ///     deletes only crate-issued gear, leaving players' own items and map props untouched.
    ///     Leave false for vanilla surplus crates.
    /// </summary>
    [DataField]
    public bool MarkIssuedItems;
}
