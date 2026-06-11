using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Clothing.AdrenalineVest;

/// <summary>
/// Goes on an armor vest. When the wearer's health drops below
/// <see cref="HealthFraction"/>, the vest kicks in: a one-time
/// <see cref="Healing"/> burst plus a short "fight or flight" rush
/// (speed boost + incoming damage reduction, see <see cref="AdrenalineRushComponent"/>).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AdrenalineVestComponent : Component
{
    /// <summary>
    /// Health fraction (of the critical threshold) below which the vest triggers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HealthFraction = 0.4f;

    /// <summary>
    /// How long the rush lasts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RushDuration = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Minimum time between activations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Earliest time the vest may trigger again.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? NextActivation;

    /// <summary>
    /// Movement speed multiplier during the rush.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.25f;

    /// <summary>
    /// Incoming damage multiplier during the rush.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DamageCoefficient = 0.85f;

    /// <summary>
    /// One-time healing applied when the vest triggers. Values should be negative.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier Healing = new();

    /// <summary>
    /// Current wearer, if equipped to a torso slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;

    [DataField]
    public SoundSpecifier ActivationSound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg")
    {
        Params = AudioParams.Default.WithVolume(2f)
    };
}

/// <summary>
/// Applied to the wearer while an adrenaline rush is active.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AdrenalineRushComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.25f;

    [DataField, AutoNetworkedField]
    public float DamageCoefficient = 0.85f;
}
