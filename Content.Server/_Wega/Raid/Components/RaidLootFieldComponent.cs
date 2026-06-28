using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Raid.Components;

/// <summary>
/// «Лут-поле»: один маркер на карте рейда, который при инициализации разбрасывает добычу по случайным
/// свободным напольным тайлам своего грида и самоудаляется. Избавляет от ручной расстановки каждой
/// лут-точки — лут появляется случайно по всей локации.
///
/// На каждый выбранный тайл спавнится случайный тир-спавнер из <see cref="Spawners"/> (взвешенный
/// повторами id), а уже он катит свой шанс, спавнит предмет и метит его как добычу рейда.
/// </summary>
[RegisterComponent]
public sealed partial class RaidLootFieldComponent : Component
{
    /// <summary>Сколько лут-точек разбросать по гриду (ограничивается числом свободных тайлов).</summary>
    [DataField]
    public int Count = 40;

    /// <summary>
    /// Взвешенный пул тир-спавнеров (<c>RaidLootSpawnerCommon/Rare/Epic</c>). Чтобы поднять долю тира,
    /// повтори его id несколько раз — выбор равновероятен по элементам списка.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Spawners = new();
}
