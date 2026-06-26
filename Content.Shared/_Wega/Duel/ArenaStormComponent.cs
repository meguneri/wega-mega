using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Duel;

/// <summary>
/// «Шторм» (battle-royale) на дуэльной арене. Живёт на том же трекере, что и
/// <c>DuelArenaComponent</c>: когда бой активен, безопасная зона — круг с центром в позиции трекера —
/// постепенно сжимается к центру, а всё за её пределами получает периодический урон. Цель — давить
/// кемперов и форсить развязку. Центр зоны = собственная позиция сущности (как у авто-дропа снабжения),
/// поэтому отдельно его сетить не нужно: клиент берёт <c>Transform.MapPosition</c> этой сущности.
///
/// Сетятся только динамические <see cref="Active"/> и <see cref="CurrentRadius"/> — по ним клиентский
/// оверлей рисует границу зоны. Остальное — серверная конфигурация и тайминги.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArenaStormComponent : Component
{
    /// <summary>Включён ли шторм для этой арены. false — компонент висит, но ничего не делает.</summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Шторм активен прямо сейчас (зона уже наступает и наносит урон). Сетится для клиентского оверлея.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active;

    /// <summary>
    /// Текущий радиус безопасной зоны (в метрах от центра). Сетится для клиентского оверлея.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float CurrentRadius;

    /// <summary>Стартовый радиус безопасной зоны в начале шторма (в метрах). Должен покрывать арену.</summary>
    [DataField]
    public float InitialRadius = 28f;

    /// <summary>Минимальный радиус, до которого зона может сжаться.</summary>
    [DataField]
    public float MinRadius = 4f;

    /// <summary>На сколько метров зона сжимается за один шаг.</summary>
    [DataField]
    public float ShrinkStep = 4f;

    /// <summary>Интервал между шагами сжатия (в секундах).</summary>
    [DataField]
    public float ShrinkInterval = 12f;

    /// <summary>Задержка после старта боя до начала наступления шторма (в секундах).</summary>
    [DataField]
    public float StartDelay = 30f;

    /// <summary>Как часто (в секундах) шторм наносит урон бойцам вне зоны.</summary>
    [DataField]
    public float DamageInterval = 1f;

    /// <summary>Урон за один тик бойцу, оказавшемуся вне безопасной зоны.</summary>
    [DataField]
    public DamageSpecifier? Damage;

    /// <summary>Объявлять ли в чат начало наступления шторма.</summary>
    [DataField]
    public bool Announce = true;

    /// <summary>Звук в момент начала наступления шторма (сужения зоны).</summary>
    [DataField]
    public SoundSpecifier? StormSound = new SoundPathSpecifier("/Audio/_Wega/Duel/duel_storm.ogg");

    // --- Серверные тайминги (не сетятся) ---

    /// <summary>Прошлое значение <c>DuelArenaComponent.IsActive</c> — для детекта старта/конца боя.</summary>
    public bool WasDuelActive;

    /// <summary>
    /// Сужение отменено на текущий бой (сигналом на порт <c>StormCancel</c>). Драйвер не заводит шторм,
    /// пока флаг стоит; сбрасывается в начале следующего боя или при сигнале <c>StormStart</c>.
    /// </summary>
    public bool Cancelled;

    /// <summary>Когда шторм начнёт наступать (после <see cref="StartDelay"/>). null — не запланирован.</summary>
    public TimeSpan? StartAt;

    /// <summary>Время следующего шага сжатия.</summary>
    public TimeSpan NextShrinkAt;

    /// <summary>Время следующего тика урона.</summary>
    public TimeSpan NextDamageAt;
}
