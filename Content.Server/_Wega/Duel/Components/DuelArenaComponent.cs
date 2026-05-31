using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Server._Wega.Duel.Components;

[RegisterComponent]
public sealed partial class DuelArenaComponent : Component
{
    /// <summary>
    /// Радиус (в тайлах) от трекера, в котором считаются дуэлянты.
    /// Должен покрывать всю арену — ставь трекер в центр.
    /// </summary>
    [DataField]
    public float ScanRange = 200f;

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
    /// Дуэль «вооружена»: в зоне есть пара бойцов и ждём исхода.
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
}
