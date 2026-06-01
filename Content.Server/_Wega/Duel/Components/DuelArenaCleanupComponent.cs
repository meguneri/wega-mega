using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Duel.Components;

/// <summary>
/// Контроллер очистки дуэльной арены. По сигналу на <see cref="TriggerPort"/> удаляет в радиусе
/// только предметы, выданные ящиком-арсеналом (помеченные <c>ArenaIssuedItemComponent</c>), где бы
/// они ни находились — на полу, в руках, надетые или внутри контейнеров. Вещи игроков и предметы
/// карты не трогаются. См. <see cref="Systems.DuelArenaCleanupSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class DuelArenaCleanupComponent : Component
{
    /// <summary>
    /// Радиус (в тайлах) очистки от контроллера. Удаляются только выданные ящиком предметы, так что
    /// чужие вещи за пределами арены не пострадают; всё же лучше ставить радиус по размеру арены.
    /// </summary>
    [DataField]
    public float Range = 200f;

    /// <summary>
    /// Приёмный порт, сигнал на который запускает очистку.
    /// </summary>
    [DataField]
    public string TriggerPort = "Trigger";
}
