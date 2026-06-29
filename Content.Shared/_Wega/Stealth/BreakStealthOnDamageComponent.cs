using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Stealth;

/// <summary>
///     When placed on a clothing item with <c>ItemToggle</c> (e.g. the infiltrator
///     hardsuit's phase cloak), the toggle is forcibly deactivated whenever the wearer
///     does anything combative — a melee attack, a shot or a throw — or takes damage.
///     The cloak only holds while the wearer is passive. Handled by
///     <see cref="Content.Server._Wega.Stealth.BreakStealthOnDamageSystem"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BreakStealthOnDamageComponent : Component { }
