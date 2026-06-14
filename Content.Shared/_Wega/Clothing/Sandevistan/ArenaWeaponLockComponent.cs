using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Clothing.Sandevistan;

/// <summary>
/// Marker placed on the arena Sandevistan eyewear: while it is worn, the wearer is locked out of
/// every weapon except those flagged <see cref="ArenaAllowedWeaponComponent"/> (the gloves of the
/// north star). Equipping grants <see cref="ArenaWeaponLockComponent"/> to the wearer; unequipping
/// removes it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SandevistanArenaLockComponent : Component
{
}

/// <summary>
/// Active weapon lock on a mob: cancels melee with any weapon that isn't flagged
/// <see cref="ArenaAllowedWeaponComponent"/>, and blocks firing all guns. Unarmed attacks are still
/// allowed. Granted/removed by the arena Sandevistan eyewear (<see cref="SandevistanArenaLockComponent"/>).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ArenaWeaponLockComponent : Component
{
}

/// <summary>
/// Flags a weapon as usable while an <see cref="ArenaWeaponLockComponent"/> is active (i.e. the
/// gloves of the north star). Everything without this marker is blocked.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ArenaAllowedWeaponComponent : Component
{
}
