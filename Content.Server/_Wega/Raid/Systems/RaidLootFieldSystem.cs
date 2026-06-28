using Content.Server._Wega.Raid.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Content.Server._Wega.Raid.Systems;

/// <summary>
/// Разбрасывает наполнение рейда по карте: <see cref="Populate"/> собирает свободные напольные тайлы
/// грида поля, выбирает из них случайные и спавнит на них случайные одноразовые спавнеры из
/// <see cref="RaidLootFieldComponent.Spawners"/> (тир-спавнеры лута и/или спавнеры скавов), которые
/// дальше сами катят шанс, спавнят содержимое и самоудаляются.
///
/// Поле НЕ срабатывает само по загрузке карты и НЕ самоудаляется — его на старте каждого рейда
/// триггерит <see cref="RaidControllerSystem"/> (через <see cref="Populate"/>), поэтому лут и скавы
/// появляются заново на каждый заход, а не один раз за раунд.
/// </summary>
public sealed partial class RaidLootFieldSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedMapSystem _map = default!;

    /// <summary>
    /// Разбрасывает спавнеры поля по случайным свободным тайлам его грида. Вызывается контроллером
    /// рейда на старте каждого рейда. Поле остаётся на карте для следующих заходов.
    /// </summary>
    public void Populate(EntityUid uid, RaidLootFieldComponent comp)
    {
        if (comp.Count <= 0 || comp.Spawners.Count == 0)
            return;

        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            Log.Warning("[raid] RaidLootField не на гриде — разброс наполнения отменён");
            return;
        }

        // Свободные напольные тайлы: не пустые/космос и без анкоренных структур (стен/мебели).
        var free = new List<Vector2i>();
        var anchored = new List<EntityUid>();
        foreach (var tile in _map.GetAllTiles(gridUid, grid))
        {
            if (tile.Tile.IsEmpty)
                continue;

            anchored.Clear();
            _map.GetAnchoredEntities((gridUid, grid), tile.GridIndices, anchored);
            if (anchored.Count > 0)
                continue;

            free.Add(tile.GridIndices);
        }

        if (free.Count == 0)
            return;

        _random.Shuffle(free);
        var count = Math.Min(comp.Count, free.Count);
        for (var i = 0; i < count; i++)
        {
            var coords = _map.GridTileToLocal(gridUid, grid, free[i]);
            Spawn(_random.Pick(comp.Spawners), coords);
        }
    }
}
