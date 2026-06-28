using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Raid.Components;

/// <summary>
/// Метка добычи рейда. Вешается на предметы, заспавненные лут-спавнерами рейда
/// (<see cref="RaidLootSpawnerComponent"/>), и на их содержимое. При экстракте суммируется
/// карго-стоимость вынесенных предметов с этой меткой — это и есть «вынос лута»: лут «продаётся»
/// (списывается), а игрок получает награду телекристаллами. Стартовое снаряжение игрока метки не
/// имеет и остаётся при нём.
/// </summary>
[RegisterComponent]
public sealed partial class RaidLootComponent : Component
{
}
