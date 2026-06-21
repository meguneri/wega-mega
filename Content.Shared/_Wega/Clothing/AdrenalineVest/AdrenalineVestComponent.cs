using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Clothing.AdrenalineVest;

/// <summary>
/// Goes on an armor vest. When the wearer's health drops below
/// <see cref="HealthFraction"/>, the vest kicks in: a healing burst
/// (<see cref="HealAmount"/> spread over <see cref="HealDuration"/>)
/// plus a short "fight or flight" rush
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
    /// How long the rush (speed boost + damage resistance) lasts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RushDuration = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Over how long the <see cref="Healing"/> is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan HealDuration = TimeSpan.FromSeconds(3);

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
    /// Total amount of damage healed over <see cref="HealDuration"/> when the
    /// vest triggers, distributed evenly across the wearer's existing damage.
    /// Positive number.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 HealAmount = FixedPoint2.Zero;

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
    /// <summary>
    /// When the speed boost and damage resistance end.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.2f;

    [DataField, AutoNetworkedField]
    public float DamageCoefficient = 0.85f;

    /// <summary>
    /// Healing applied per second while the heal-over-time is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 HealPerSecond = FixedPoint2.Zero;

    /// <summary>
    /// Fractional healing carried over between ticks, so small per-tick
    /// amounts are not lost to rounding.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HealAccumulator;

    /// <summary>
    /// When the heal-over-time ends.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan HealEndTime;
}
