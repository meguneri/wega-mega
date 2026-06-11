using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Weapons.Parry;

/// <summary>
/// Goes on a blocking item (shield). Getting melee-attacked while actively
/// blocking "primes" a riposte: the next melee attack made with this item
/// within <see cref="RiposteWindow"/> deals <see cref="RiposteBonusDamage"/>
/// and <see cref="RiposteStaminaDamage"/> to the target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParryRiposteComponent : Component
{
    /// <summary>
    /// How long after a successful parry the riposte stays available.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RiposteWindow = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Until when the riposte is primed. Null when not primed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? PrimedUntil;

    /// <summary>
    /// Extra damage added to the primed attack.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier RiposteBonusDamage = new();

    /// <summary>
    /// Stamina damage dealt to each entity hit by the primed attack.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RiposteStaminaDamage = 25f;

    /// <summary>
    /// Played when a parry primes the riposte.
    /// </summary>
    [DataField]
    public SoundSpecifier ParrySound = new SoundPathSpecifier("/Audio/Weapons/block_metal1.ogg")
    {
        Params = AudioParams.Default.WithPitchScale(1.35f).WithVolume(2f)
    };
}
