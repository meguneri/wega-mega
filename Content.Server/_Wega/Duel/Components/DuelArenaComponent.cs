using Robust.Shared.GameObjects;

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
    /// Зарегистрированные дуэлянты текущего боя.
    /// </summary>
    public readonly HashSet<EntityUid> Duelists = new();

    /// <summary>
    /// Дуэль «вооружена»: в зоне есть пара бойцов и ждём исхода.
    /// </summary>
    public bool IsActive;

    /// <summary>
    /// Время следующего сканирования.
    /// </summary>
    public TimeSpan NextScan;
}
