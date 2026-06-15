using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Evasion;

/// <summary>
/// Gives the wearer/holder a chance to completely dodge an incoming attack, negating all of its
/// damage. Relayed through the inventory like <see cref="Content.Shared.Armor.ArmorComponent"/>, so
/// it works when placed on a piece of clothing (e.g. the arena Sandevistan implant). _Wega
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(EvasionSystem))]
public sealed partial class EvasionComponent : Component
{
    /// <summary>Chance (0..1) to fully evade a single incoming attack.</summary>
    [DataField]
    public float Chance = 0.15f;
}
