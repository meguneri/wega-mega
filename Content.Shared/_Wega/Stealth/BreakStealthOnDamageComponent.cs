using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Stealth;

/// <summary>
///     When placed on a clothing item with <c>ItemToggle</c> (e.g. the infiltrator
///     hardsuit's phase cloak), the toggle is forcibly deactivated whenever the
///     wearer takes damage.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BreakStealthOnDamageComponent : Component { }
