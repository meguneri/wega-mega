using Robust.Shared.GameStates;

namespace Content.Shared._Wega.CannotPickup;

/// <summary>
/// Entities with this component can never pick items up into their hands.
/// Used for ability-only mobs like Сатору Годжо who fight with innate powers
/// and must not grab dropped weapons.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CannotPickupComponent : Component;
