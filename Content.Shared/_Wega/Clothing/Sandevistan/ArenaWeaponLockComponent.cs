using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
    /// <summary>
    /// How many independent sources currently impose the lock (worn arena eyewear and/or the arena
    /// implant). The component — and thus the lock — lifts only when this drops back to zero, so
    /// taking off the glasses never removes a lock the implant is still holding. Server-side bookkeeping.
    /// </summary>
    public int Sources;
}

/// <summary>
/// Flags a weapon as usable while an <see cref="ArenaWeaponLockComponent"/> is active (i.e. the
/// gloves of the north star). Everything without this marker is blocked.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArenaAllowedWeaponComponent : Component
{
    /// <summary>
    /// Count of landed hits while the wielder wears the arena Sandevistan. Every 3rd hit lands an
    /// armour-piercing strike instead of the normal blow (see <see cref="ArmorPierceEveryNthHit"/>).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int HitCount;

    /// <summary>Every N-th landed hit pierces armour.</summary>
    [DataField]
    public int ArmorPierceEveryNthHit = 3;

    /// <summary>Blunt damage dealt by the armour-piercing strike (ignores resistances).</summary>
    [DataField]
    public float PierceDamage = 10f;

    /// <summary>Sound played on the target when the armour-piercing strike lands.</summary>
    [DataField]
    public SoundSpecifier PierceSound = new SoundPathSpecifier("/Audio/Weapons/pierce.ogg");

    /// <summary>
    /// Hit sound used while the wielder wears a Sandevistan (<see cref="SandevistanWearerComponent"/>):
    /// a heavy metallic thud, like striking with iron gauntlets, instead of the default sharp punch.
    /// Slightly lowered pitch so it reads as weighty iron rather than a ringing clang — no new assets.
    /// </summary>
    [DataField]
    public SoundSpecifier SandevistanHitSound = new SoundCollectionSpecifier("MetalThud")
    {
        Params = AudioParams.Default.WithPitchScale(0.9f).WithVolume(-1f)
    };

    /// <summary>Visual effect spawned on the target when the armour-piercing strike lands.</summary>
    [DataField]
    public EntProtoId PierceEffect = "EffectSparks";
}
