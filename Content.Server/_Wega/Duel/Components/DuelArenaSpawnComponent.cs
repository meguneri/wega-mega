using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Duel.Components;

/// <summary>
/// Невидимый маркер точки появления бойца на арене (ставится при маппинге, по 2+ на каждую
/// арену-карту). Контроллер ротации (<see cref="DuelRotationComponent"/>) после загрузки арены
/// собирает все такие маркеры на её гриде и телепортирует на них дуэлянтов в начале раунда.
///
/// На одиночную арену (без контроллера ротации) никак не влияет — маркеры просто игнорируются.
/// </summary>
[RegisterComponent]
public sealed partial class DuelArenaSpawnComponent : Component
{
}
