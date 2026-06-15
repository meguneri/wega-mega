using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;

namespace Content.Shared._Wega.Clothing.Sandevistan;

/// <summary>
/// Cyberware (worn item / implant). Grants the wearer an action that triggers a short
/// "bullet time" burst — a big movement-speed boost plus incoming-damage reduction
/// (<see cref="SandevistanActiveComponent"/>) — on a cooldown.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanComponent : Component
{
    /// <summary>How long the speed burst lasts.</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(6);

    /// <summary>Minimum time between activations.</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    /// <summary>Earliest time it may be activated again.</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? NextActivation;

    /// <summary>Walk/sprint speed multiplier while active.</summary>
    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.8f;

    /// <summary>Incoming damage multiplier while active (reaction time makes you harder to hit).</summary>
    [DataField, AutoNetworkedField]
    public float DamageCoefficient = 0.85f;

    /// <summary>Radius (tiles) within which other mobs are slowed to a crawl ("bullet time").
    /// Ignored when <see cref="AffectWholeMap"/> is true.</summary>
    [DataField, AutoNetworkedField]
    public float SlowRadius = 6f;

    /// <summary>
    /// If true, the slow affects EVERYTHING on the user's map (every mob and projectile), not just
    /// entities within <see cref="SlowRadius"/> — full "bullet time".
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AffectWholeMap = true;

    /// <summary>Walk/sprint multiplier applied to slowed mobs while active.</summary>
    [DataField, AutoNetworkedField]
    public float SlowModifier = 0.35f;

    /// <summary>Time between trailing afterimages spawned while active.</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AfterimageInterval = TimeSpan.FromSeconds(0.1);

    /// <summary>How long each afterimage lingers before fading out.</summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AfterimageLifetime = TimeSpan.FromSeconds(0.6);

    /// <summary>Action granted to the wearer.</summary>
    [DataField]
    public EntProtoId Action = "ActionSandevistan";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>Current wearer, if equipped.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;

    [DataField]
    public SoundSpecifier ActivationSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg");
}

/// <summary>
/// Temporary buff applied to the wearer while the Sandevistan is active: speed boost and
/// damage reduction. Removed automatically when <see cref="EndTime"/> passes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanActiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public float SpeedModifier = 1.8f;

    [DataField, AutoNetworkedField]
    public float DamageCoefficient = 0.85f;

    [DataField, AutoNetworkedField]
    public float SlowRadius = 6f;

    [DataField, AutoNetworkedField]
    public bool AffectWholeMap = true;

    [DataField, AutoNetworkedField]
    public float SlowModifier = 0.35f;

    // Afterimage trail ("David Martinez" blue blur).
    [DataField, AutoNetworkedField]
    public TimeSpan AfterimageInterval = TimeSpan.FromSeconds(0.1);

    [DataField, AutoNetworkedField]
    public TimeSpan AfterimageLifetime = TimeSpan.FromSeconds(0.6);

    [DataField, AutoNetworkedField]
    public TimeSpan NextAfterimageTime;
}

/// <summary>
/// A trailing "ghost" left behind a moving Sandevistan user. The client copies the user's
/// sprite onto it (translucent, rainbow-tinted); it fades out via TimedDespawn.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanAfterimageComponent : Component
{
    /// <summary>The user this afterimage was copied from.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid SourceEntity;

    /// <summary>Facing the user had when this afterimage was spawned.</summary>
    [DataField, AutoNetworkedField]
    public Direction DirectionOverride;
}

/// <summary>
/// Applied to mobs caught in an active Sandevistan user's radius — slows them while present.
/// Continuously refreshed in range; expires shortly after leaving it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandevistanSlowedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime;

    [DataField, AutoNetworkedField]
    public float SlowModifier = 0.35f;
}

/// <summary>
/// Applied to projectiles caught in an active Sandevistan's bullet-time: their velocity is scaled
/// down by <see cref="Factor"/> while active and restored (divided back) when the burst ends or the
/// projectile leaves influence. <see cref="Factor"/> is stored so the restore is exact even if the
/// slow modifier ever changes.
/// </summary>
[RegisterComponent]
public sealed partial class SandevistanSlowedProjectileComponent : Component
{
    [DataField]
    public TimeSpan EndTime;

    /// <summary>The multiplier already applied to this projectile's velocity (e.g. 0.35).</summary>
    [DataField]
    public float Factor = 1f;
}

/// <summary>Raised on the cyberware item when its action is used.</summary>
public sealed partial class SandevistanActivateEvent : InstantActionEvent;
