using Robust.Shared.GameObjects;

namespace Content.Shared._Wega.Raid.Components;

[RegisterComponent]
public sealed partial class RaidSkavAccuracyComponent : Component
{
    /// <summary>Конус «достаточного прицеливания» в градусах (выше — косее). Дефолт движка — 30.</summary>
    [DataField]
    public float AccuracyDegrees = 30f;

    /// <summary>Задержка между выстрелами в секундах. null — не менять (дефолт ~0.2).</summary>
    [DataField]
    public float? ShootDelay;

    /// <summary>Скорость доворота на цель в градусах/сек. null — мгновенно.</summary>
    [DataField]
    public float? RotationSpeedDegrees;

    /// <summary>Меткость уже применена к NPCRangedCombatComponent — чтобы не применять повторно.</summary>
    [ViewVariables]
    public bool Applied;
}
