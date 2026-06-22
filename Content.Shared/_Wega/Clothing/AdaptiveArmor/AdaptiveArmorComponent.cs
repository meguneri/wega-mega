using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Clothing.AdaptiveArmor;

/// <summary>
/// Raised on a target that is about to be hit by an armour-piercing projectile — one with
/// <c>IgnoreResistances</c>, which skips the normal <see cref="Content.Shared.Damage.Systems.DamageModifyEvent"/>
/// armour pass entirely. Lets adaptive plating still learn from and soften AP rounds the same way it does
/// ordinary hits. Handlers mutate <see cref="Damage"/> in place; the projectile system fires the resulting
/// damage. Server-side only (projectile collisions resolve on the server).
/// </summary>
[ByRefEvent]
public record struct ArmorPiercingHitEvent(DamageSpecifier Damage, EntityUid? Origin);

/// <summary>Marks the Mahoraga wheel effect so the client can drive its per-type tint, segment gauge and
/// the bright flash it pops on every adaptation. The wheel's ring/spokes/glow spin on their own (looping
/// RSI), so we only react to discrete state changes here.</summary>
[RegisterComponent]
public sealed partial class AdaptiveWheelComponent : Component
{
    /// <summary>Last spin counter we played a flash for, so a repeated appearance sync doesn't re-trigger.</summary>
    public int LastSpin;

    /// <summary>Colour the glow settles to between flashes — the currently adapted damage type, or gold when idle.</summary>
    public Color TypeColor = AdaptiveArmorColors.Default;

    /// <summary>Seconds left in the current absorb/adapt flash; the glow eases from white back to <see cref="TypeColor"/>.</summary>
    public float FlashRemaining;

    /// <summary>Total length of the active flash, for normalising the ease.</summary>
    public float FlashDuration;

    /// <summary>Seconds left in the current ratchet; the spokes advance one notch (45°) and settle, like
    /// Mahoraga's wheel clicking round a slot on each adaptation.</summary>
    public float SpinRemaining;

    /// <summary>Total length of the active ratchet, for normalising the ease.</summary>
    public float SpinDuration;

    /// <summary>Where the spokes currently rest (radians). Each adaptation adds a 45° notch; the spokes hold
    /// here between adaptations rather than springing back.</summary>
    public float SpokeAngle;

    /// <summary>The rest angle the current ratchet started from, for interpolating the click.</summary>
    public float SpinStartAngle;

    /// <summary>Settled colour for each of the 8 glow sectors; set from ActiveTypes in OnAppearanceChange
    /// and used to restore sector colours after a flash fades.</summary>
    public Color[] SectorColors = new Color[8];
}

/// <summary>Marks the one-shot expanding shockwave spawned when a blow is absorbed, so the client can tint it
/// by the absorbed damage type.</summary>
[RegisterComponent]
public sealed partial class AdaptiveShockwaveComponent : Component;

/// <summary>Appearance keys on the wheel/shockwave. The server bumps <see cref="Spin"/> each time the armour
/// adapts (with <see cref="Strong"/> set when the blow was actually absorbed) so the client ratchets the
/// spokes, and pushes the live <see cref="Type"/> so it can colour the shockwave, plus the full
/// <see cref="ActiveTypes"/> list so the client can colour each 45° sector of the glow independently.</summary>
[Serializable, NetSerializable]
public enum AdaptiveWheelVisuals : byte
{
    Spin,
    Strong,
    Type,
    /// <summary>Comma-separated string of significant adapted damage type ids, sorted alphabetically. Empty
    /// string when idle. Only includes types with a distinct colour and damage ≥ 30% of the dominant type.
    /// Drives per-sector colouring on the wheel (sector i gets the colour of types[i*N/8]).</summary>
    ActiveTypes,
}

/// <summary>Maps damage type ids to the colour the wheel, shockwave and armour accent glow with, so a glance
/// tells you what the plating is currently hardened against.</summary>
public static class AdaptiveArmorColors
{
    /// <summary>Idle / unknown-type tint — the wheel's resting gold.</summary>
    public static readonly Color Default = Color.FromHex("#FFC83C");

    // Deliberately well-separated hues so each type reads at a glance — Slash is red, not white, so it no
    // longer looks like Piercing's yellow.
    private static readonly Dictionary<string, Color> Map = new()
    {
        ["Blunt"] = Color.FromHex("#DCE2EA"),     // silver-white
        ["Slash"] = Color.FromHex("#FF1A4B"),     // crimson-pink (clearly cooler than Heat's orange)
        ["Piercing"] = Color.FromHex("#FFC81E"),  // amber-yellow
        ["Heat"] = Color.FromHex("#FF7A00"),      // warm orange
        ["Shock"] = Color.FromHex("#22CCF2"),     // cyan
        ["Cold"] = Color.FromHex("#8FD4FF"),      // light blue
        ["Caustic"] = Color.FromHex("#6FE03A"),   // green
        ["Poison"] = Color.FromHex("#9AB820"),    // olive
        ["Radiation"] = Color.FromHex("#B45CFF"), // violet
        // Synthetic adaptation keys — not real damage types, but explosions and armour-piercing rounds both
        // bypass the normal damage pipeline (ignoreResistances), so the armour tracks each as its own learned
        // threat. Explosion is vermilion blast-red; armour-piercing is gunmetal — both kept clear of the real
        // damage hues above.
        ["Explosion"] = Color.FromHex("#FF4500"),
        ["ArmorPiercing"] = Color.FromHex("#6E7B8B"),
    };

    public static Color ForType(string? type)
        => type != null && Map.TryGetValue(type, out var color) ? color : Default;

    /// <summary>True when the type has a dedicated, non-default colour. Types not in the map (e.g. Structural)
    /// fall back to the idle gold and are excluded from sector colouring.</summary>
    public static bool HasDistinctColor(string type) => Map.ContainsKey(type);
}

/// <summary>
/// Config component on an "adaptive plating" arena vest. While worn it attaches an
/// <see cref="AdaptiveArmorActiveComponent"/> to the wearer: the armour "learns" the damage types it
/// is being hit by and ramps up resistance against each of them for a short window, so an attacker has to
/// keep introducing fresh damage types to stay effective.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AdaptiveArmorComponent : Component
{
    /// <summary>How long an adaptation to a damage type lasts (refreshed by each matching hit).</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AdaptDuration = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Multiplier applied to incoming damage of the currently-adapted type, on top of the vest's flat
    /// <c>Armor</c>. 0.25 stacked on a 0.8 flat coefficient ≈ 0.2 taken, i.e. ~80% of that type absorbed
    /// while the adaptation holds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AdaptCoefficient = 0.25f;

    /// <summary>Current wearer, if equipped to a torso slot.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;
}

/// <summary>
/// Applied to the wearer while an adaptive vest is worn; holds the live adaptation state and applies
/// the bonus resistance through <see cref="Content.Shared.Damage.DamageModifyEvent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AdaptiveArmorActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan AdaptDuration = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public float AdaptCoefficient = 0.25f;

    /// <summary>
    /// Every damage type the armour is currently adapted to, mapped to when that adaptation expires.
    /// A hit teaches the armour <em>all</em> of its damage types at once, so multi-type weapons (an energy
    /// sword's Slash + Heat, mixed shotgun shot) can't keep slipping a second type through full-force; each
    /// type is refreshed by a matching hit and pruned once its own window passes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, TimeSpan> AdaptedTypes = new();

    /// <summary>Last recorded incoming damage per type (pre-mitigation snapshot, updated each hit).
    /// Server-only: used to filter noise from the wheel display so minor incidental types (e.g. Blunt 4.5
    /// on an energy sword) don't pollute the sector colours. Not networked — only the resulting
    /// comma-string is sent via appearance data.</summary>
    [DataField]
    public Dictionary<string, float> AdaptedAmounts = new();

    /// <summary>The vest itself, so we can drive its emissive accent (ToggleableVisuals) by adapted type.</summary>
    [DataField]
    public EntityUid? Vest;

    /// <summary>The persistent Mahoraga-style wheel hovering over the wearer's head while the armour is worn.
    /// Server-side bookkeeping so it can be cleaned up on unequip; the entity itself replicates to clients
    /// and follows the wearer through its parented transform.</summary>
    [DataField]
    public EntityUid? WheelEffect;

    /// <summary>How many times the wheel has been told to flash; pushed to the wheel's appearance so the
    /// client plays one flash per increment.</summary>
    [DataField]
    public int WheelSpin;

    /// <summary>Whether the emissive accent / wheel tint is currently lit, so the lapse cleanup runs once.</summary>
    [DataField]
    public bool GlowActive;
}
