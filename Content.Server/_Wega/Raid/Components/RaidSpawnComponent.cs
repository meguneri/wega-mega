using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Raid.Components;

/// <summary>
/// Невидимый маркер точки появления рейдера на карте рейда (точка инсерта). Ставится при маппинге,
/// несколько штук по краям локации. Кнопка входа (<see cref="RaidEntryComponent"/>) при нажатии
/// собирает маркеры на карте рейда и раскидывает по ним вошедших игроков.
///
/// По образцу <c>DuelArenaSpawnComponent</c>.
/// </summary>
[RegisterComponent]
public sealed partial class RaidSpawnComponent : Component
{
    /// <summary>Номер точки для предсказуемого порядка раздачи спавнов при общем входе.</summary>
    [DataField]
    public int SpawnIndex;
}
