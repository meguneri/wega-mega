using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Clothing.Sandevistan;

/// <summary>
/// Native, always-on version of the arena Sandevistan's passive perks: a standing incoming-damage
/// reduction, a flat dodge chance and a small permanent speed boost. The worn eyewear gets the same
/// perks from clothing components (<c>Armor</c> / <c>Evasion</c> / <c>ClothingSpeedModifier</c>),
/// which only work through inventory slots. The implant variant adds this component directly to the
/// mob (via <c>SubdermalImplant.implantComponents</c>) so the perks apply without any worn item.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanArenaPassiveComponent : Component
{
    /// <summary>Incoming-damage multiplier (passive armour). 0.85 = 15% reduction.</summary>
    [DataField, AutoNetworkedField]
    public float DamageCoefficient = 0.85f;

    /// <summary>Chance (0–1) to fully dodge each incoming attack.</summary>
    [DataField, AutoNetworkedField]
    public float DodgeChance = 0.08f;

    /// <summary>Passive walk/sprint speed multiplier (applies even between bursts).</summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.1f;
}
