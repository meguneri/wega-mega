using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Raid.Components;

/// <summary>
/// Кнопка входа в рейд (на базе <c>SignalButton</c>), ставится на хаб. При нажатии собирает всех
/// мобов на гриде кнопки и телепортирует их на спавн-маркеры карты рейда (<see cref="RaidSpawnComponent"/>),
/// добавляя в список рейдеров и запуская таймер рейда, если он ещё не идёт.
///
/// Контроллер рейда (<see cref="RaidControllerComponent"/>) ищется первым на сервере — он один.
/// По образцу <c>DuelArenaEntryComponent</c> (общий вход).
/// </summary>
[RegisterComponent]
public sealed partial class RaidEntryComponent : Component
{
}
