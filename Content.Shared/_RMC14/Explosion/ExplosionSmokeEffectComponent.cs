using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

/// <summary>
///     Smoke puff spawned after an explosion. The client animates it (expand + fade).
///     Ported from lust-station / RMC14 (Sunrise).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExplosionSmokeEffectComponent : Component
{
    public const float AnimationDuration = 2.5f;
    public const float Variation = 1f;

    [DataField, AutoNetworkedField]
    public float LifeTime = AnimationDuration;
}

/// <summary>
///     Animated explosion sprite spawned at the blast point (scales up + auto-animates).
///     Ported from lust-station / RMC14 (Sunrise).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ExplosionEffectComponent : Component
{
    public const float AnimationDuration = 2.5f;

    [DataField, AutoNetworkedField]
    public float LifeTime = AnimationDuration;

    [DataField, AutoNetworkedField]
    public float SizeModifier = 2f;
}
