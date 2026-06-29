using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Duel.Components;

/// <summary>
/// Случайные авиаудары во время активной дуэли. Живёт на том же трекере, что и
/// <c>DuelArenaComponent</c>. За <see cref="WarningDuration"/> секунд до каждого удара
/// на тайле появляется маркер-прицел, затем следует взрыв. Удары падают в радиусе
/// <see cref="StrikeRadius"/> тайлов от случайного живого дуэлянта.
/// </summary>
[RegisterComponent]
public sealed partial class ArenaAirstrikeComponent : Component
{
    /// <summary>Включён ли модуль для этой арены.</summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>Задержка после старта боя до первой волны (секунды).</summary>
    [DataField]
    public float FirstStrikeDelay = 20f;

    /// <summary>Интервал между волнами ударов (секунды).</summary>
    [DataField]
    public float StrikeInterval = 15f;

    /// <summary>Время от появления прицела до взрыва (секунды).</summary>
    [DataField]
    public float WarningDuration = 3f;

    /// <summary>Число одновременных ударов в одной волне.</summary>
    [DataField]
    public int StrikeCount = 2;

    /// <summary>Максимальный радиус (в тайлах) вокруг каждого бойца для выбора точки удара.</summary>
    [DataField]
    public int StrikeRadius = 10;

    /// <summary>Тип взрыва (id прототипа explosion).</summary>
    [DataField]
    public string ExplosionType = "RMCGrenadeNoTileFire";

    [DataField]
    public float TotalIntensity = 150f;

    [DataField]
    public float Slope = 5f;

    [DataField]
    public float MaxTileIntensity = 10f;

    /// <summary>Прототип маркера-прицела, появляющегося перед ударом.</summary>
    [DataField]
    public EntProtoId WarningProto = "ArenaAirstrikeWarning";

    // Runtime-state

    /// <summary>Прошлое значение IsActive — для детекта старта/конца боя.</summary>
    public bool WasDuelActive;

    /// <summary>Когда запустить следующую волну. null — не запланирована (бой не идёт).</summary>
    public TimeSpan? NextStrikeAt;

    /// <summary>Ожидающие удары: маркер + время взрыва.</summary>
    public readonly List<(EntityUid Marker, TimeSpan FireAt)> PendingStrikes = new();
}
