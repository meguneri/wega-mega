using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Duel.Components;

[RegisterComponent]
public sealed partial class DuelArenaComponent : Component
{
    /// <summary>
    /// Охват арены — весь грид трекера (дуэлянты считаются по всему гриду, без радиуса).
    /// Это значение — лишь запасной радиус (в тайлах) на случай, если трекер не на гриде (в космосе).
    /// </summary>
    [DataField]
    public float ScanRange = 200f;

    /// <summary>
    /// Очистка снаряжения охватывает весь грид трекера (без радиуса).
    /// Это значение — лишь запасной радиус (в тайлах) на случай, если трекер не на гриде (в космосе).
    /// </summary>
    [DataField]
    public float CleanupRange = 200f;

    /// <summary>
    /// Как часто (в секундах) трекер сканирует зону на наличие дуэлянтов.
    /// </summary>
    [DataField]
    public float ScanInterval = 0.5f;

    /// <summary>
    /// Выходной порт, на который шлётся сигнал при завершении дуэли (закрывает барьеры).
    /// </summary>
    [DataField]
    public string ResetPort = "DuelEnded";

    /// <summary>
    /// Через сколько секунд после конца боя трекер шлёт на шлюзы баз сигнал закрытия —
    /// чтобы дуэлянты успели вернуться в свои базы по открытым шлюзам.
    /// </summary>
    [DataField]
    public float ReturnGrace = 20f;

    /// <summary>
    /// Прототип маяка снабжения, который арена сбрасывает в центр во время активного боя.
    /// null — авто-дроп выключен (дроп управляется только кнопкой-спавнером, прежнее поведение).
    /// Обычно <c>DuelSupplyDropBeacon</c>.
    /// </summary>
    [DataField]
    public EntProtoId? SupplyDropProto;

    /// <summary>
    /// Через сколько секунд после старта боя падает первый ящик снабжения.
    /// </summary>
    [DataField]
    public float SupplyDropDelay = 45f;

    /// <summary>
    /// Интервал повторных сбросов снабжения (в секундах). 0 — одноразовый сброс за бой.
    /// </summary>
    [DataField]
    public float SupplyDropInterval = 45f;

    /// <summary>
    /// Время следующего сброса снабжения во время боя. null — не запланирован.
    /// </summary>
    public TimeSpan? SupplyDropAt;

    /// <summary>
    /// Зарегистрированные дуэлянты текущего боя.
    /// </summary>
    public readonly HashSet<EntityUid> Duelists = new();

    /// <summary>
    /// Накопительный счёт побед по игрокам (NetUserId → число побед).
    /// Ключ — игрок, а не тело: счёт переживает клонирование/респавн (новый EntityUid).
    /// Сохраняется между боями на этой арене; сбрасывается сигналом на порт Reset.
    /// </summary>
    public readonly Dictionary<NetUserId, int> Scores = new();

    /// <summary>
    /// Последнее известное имя игрока (NetUserId → имя). Нужно, чтобы показывать общий счёт
    /// с именами даже тех бойцов, кого нет в текущем бою. Сбрасывается вместе со <see cref="Scores"/>.
    /// </summary>
    public readonly Dictionary<NetUserId, string> ScoreNames = new();

    /// <summary>
    /// Игрок, выигравший прошлый бой, и его текущая серия побед подряд.
    /// Серия растёт, пока побеждает тот же игрок; обнуляется при смене победителя, ничьей и сбросе счёта.
    /// </summary>
    public NetUserId? StreakUser;

    public int Streak;

    /// <summary>
    /// Дуэль «вооружена»: в зоне есть минимум двое бойцов (поддерживается 3+) и ждём исхода.
    /// </summary>
    public bool IsActive;

    /// <summary>
    /// Время следующего сканирования.
    /// </summary>
    public TimeSpan NextScan;

    /// <summary>
    /// Время, когда трекер отправит на шлюзы баз сигнал закрытия после конца боя (см. <see cref="ReturnGrace"/>).
    /// null — закрытие не запланировано (бой идёт либо шлюзы уже закрыты).
    /// </summary>
    public TimeSpan? GateCloseAt;

    /// <summary>
    /// Снимок исходной (пристайн) планировки стен арены: тайл грида → прототип стены.
    /// Снимается лениво при старте первой дуэли, пока стены ещё целы. После каждой дуэли
    /// по этому снимку восстанавливаются стены, разрушенные в ходе боя.
    /// </summary>
    public readonly Dictionary<Vector2i, EntProtoId> WallSnapshot = new();

    /// <summary>
    /// Снимок стен уже сделан — не пересоздаём его на последующих боях.
    /// </summary>
    public bool WallSnapshotTaken;
}
