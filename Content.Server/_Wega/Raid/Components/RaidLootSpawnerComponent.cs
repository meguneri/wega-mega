using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Raid.Components;

/// <summary>
/// Невидимый лут-спавнер рейда. При инициализации карты с шансом <see cref="SpawnChance"/> спавнит
/// случайные предметы из <see cref="Loot"/> (от <see cref="MinItems"/> до <see cref="MaxItems"/>),
/// помечает их (и содержимое) как добычу рейда (<see cref="RaidLootComponent"/>) и самоудаляется.
/// Стоимость предметов берётся штатной карго-оценкой — её и получает игрок при экстракте.
///
/// Ставится при маппинге по локации рейда. Разные тиры (обычный/редкий/элитный) — разные списки
/// <see cref="Loot"/> и количества.
/// </summary>
[RegisterComponent]
public sealed partial class RaidLootSpawnerComponent : Component
{
    /// <summary>Пул возможной добычи. Каждый спавн — случайный выбор из этого списка (равновероятно).</summary>
    [DataField(required: true)]
    public List<EntProtoId> Loot = new();

    /// <summary>Минимум предметов за один спавн.</summary>
    [DataField]
    public int MinItems = 1;

    /// <summary>Максимум предметов за один спавн.</summary>
    [DataField]
    public int MaxItems = 1;

    /// <summary>Шанс [0..1], что спавнер вообще что-то выдаст (пустые точки = «не повезло»).</summary>
    [DataField]
    public float SpawnChance = 1f;
}
