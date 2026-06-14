using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Duel.Components;

/// <summary>
/// Кнопка входа на арену с хаба ротации. При нажатии игроком (<c>ActivateInWorldEvent</c>)
/// собирает всех игроков на гриде кнопки (хабе), телепортирует их на спавн-маркеры арены
/// с индексом <see cref="ArenaIndex"/> (в списке <c>arenas</c> контроллера) и запускает там раунд.
///
/// На хаб ставится по одной кнопке на каждую арену (с разными ArenaIndex). Контроллер
/// <see cref="DuelRotationComponent"/> должен быть на той же карте (он держит список арен и их
/// загруженные MapId).
/// </summary>
[RegisterComponent]
public sealed partial class DuelArenaEntryComponent : Component
{
    /// <summary>
    /// Индекс арены в списке <see cref="DuelRotationComponent.Arenas"/>, на которую ведёт кнопка.
    /// </summary>
    [DataField]
    public int ArenaIndex;
}
