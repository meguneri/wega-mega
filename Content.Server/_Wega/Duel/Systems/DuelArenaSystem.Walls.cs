using System.Linq;
using Content.Server._Wega.Duel.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Восстановление стен дуэльной арены. При каждом старте дуэли (стены целы) планировка стен
/// мержится в снимок (тайл → прототип стены + тайл пола под ней). После каждого раунда стены
/// приводятся к снимку: целая правильная стена чинится (помятая становится новой); отсутствующая /
/// разрушенная до балки / чужая стена убирается и заново ставится свежей; уничтоженный пол под
/// стеной восстанавливается. Восстановление выполняется отложенно (см. PendingWallRestore) — на
/// тике после завершения боя, вне стека события смерти.
/// </summary>
public sealed partial class DuelArenaSystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    /// <summary>
    /// Мержит текущую планировку стен арены в снимок. Вызывается при КАЖДОМ старте дуэли (стены
    /// в этот момент целы): новые тайлы со стенами добавляются, уже записанные не перезаписываются.
    /// Так снимок самовосстанавливается, даже если какой-то проход вышел неполным (например, при
    /// старте боя часть стен оказалась недоступна для перечисления) — следующий старт дуэли дополнит
    /// недостающее. Заодно запоминается тайл пола под каждой стеной (для восстановления дыр).
    /// </summary>
    private void SnapshotWalls(EntityUid arenaUid, DuelArenaComponent comp)
    {
        var grid = Transform(arenaUid).GridUid;
        if (grid == null || !TryComp<MapGridComponent>(grid, out var gridComp))
        {
            Log.Warning($"[duel-arena] снимок стен невозможен: трекер {ToPrettyString(arenaUid)} не на гриде");
            return;
        }

        var added = 0;

        // Проходим по всем тегированным сущностям и берём только стены на гриде арены.
        var query = EntityQueryEnumerator<TagComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tagComp, out var xform))
        {
            if (xform.GridUid != grid || !_tag.HasTag(tagComp, WallTag))
                continue;

            if (MetaData(uid).EntityPrototype?.ID is not { } proto)
                continue;

            var tile = _map.TileIndicesFor(grid.Value, gridComp, xform.Coordinates);
            if (comp.WallSnapshot.TryAdd(tile, proto))
                added++;

            // Пол под стеной — чтобы восстановить, если за бой его уничтожат (дыра в космос).
            comp.WallTileSnapshot.TryAdd(tile, _map.GetTileRef(grid.Value, gridComp, tile).Tile);
        }

        if (added > 0)
            Log.Info($"[duel-arena] снимок стен пополнен: +{added}, всего {comp.WallSnapshot.Count} тайлов");
        else if (comp.WallSnapshot.Count == 0)
            Log.Warning($"[duel-arena] снимок стен ПУСТ: на гриде {grid} не найдено ни одной стены с тегом Wall");
    }

    /// <summary>
    /// Приводит стены арены к снимку <see cref="DuelArenaComponent.WallSnapshot"/>. Для каждого
    /// тайла снимка: если на месте стоит правильная (по прототипу) стена — лечим ей повреждения,
    /// чтобы помятая стала как новая; иначе (стены нет, она разрушена до балки или стоит чужая) —
    /// убираем балки/неправильную стену, чиним пол (если уничтожен) и ставим свежую стену.
    /// Настенные предметы (постеры, лампы, интеркомы, APC) не трогаем. Каждый тайл обрабатывается
    /// независимо (ошибка на одном не прерывает остальные). Вызывается из Update на тике после
    /// завершения боя.
    /// </summary>
    private void RestoreWalls(EntityUid arenaUid, DuelArenaComponent comp)
    {
        if (comp.WallSnapshot.Count == 0)
        {
            Log.Warning($"[duel-arena] восстановление стен пропущено: снимок пуст ({ToPrettyString(arenaUid)})");
            return;
        }

        var grid = Transform(arenaUid).GridUid;
        if (grid == null || !TryComp<MapGridComponent>(grid, out var gridComp))
        {
            Log.Warning($"[duel-arena] восстановление стен невозможно: трекер {ToPrettyString(arenaUid)} не на гриде");
            return;
        }

        var healed = 0;
        var respawned = 0;
        var blockedCount = 0;
        var failed = 0;

        var anchored = new List<EntityUid>();
        foreach (var (tile, proto) in comp.WallSnapshot)
        {
            try
            {
                anchored.Clear();
                _map.GetAnchoredEntities((grid.Value, gridComp), tile, anchored);

                // Ищем стену на тайле.
                EntityUid? wall = null;
                foreach (var e in anchored)
                {
                    if (Exists(e) && _tag.HasTag(e, WallTag))
                    {
                        wall = e;
                        break;
                    }
                }

                // Правильная стена уже стоит — просто чиним повреждения (помятая → новая) и идём дальше.
                if (wall is { } existing && MetaData(existing).EntityPrototype?.ID == proto.Id)
                {
                    // Сбрасываем урон в ноль — помятая/повреждённая стена становится как новая.
                    if (TryComp<DamageableComponent>(existing, out var damage))
                    {
                        _damageable.SetAllDamage((existing, damage), FixedPoint2.Zero);
                        healed++;
                    }
                    continue;
                }

                // Освобождаем тайл от балок (остатков снесённой стены) И от чужой/повреждённой стены
                // ВСЕГДА — даже если свежую в этот раз не поставим: иначе мусор зависнет навсегда (на
                // занятом мобом тайле его не уберёт ни клинап, ни следующее восстановление). НЕ трогаем
                // прочие заякоренные сущности — настенные постеры, лампы, интеркомы, APC и т.п. должны
                // пережить восстановление стены.
                foreach (var debris in anchored)
                {
                    if (Exists(debris) && (IsWallDebris(debris) || _tag.HasTag(debris, WallTag)))
                        Del(debris);
                }

                // Пол под стеной уничтожен (дыра в космос)? Без пола стену не заякорить —
                // восстанавливаем тайл по снимку.
                if (_map.GetTileRef(grid.Value, gridComp, tile).Tile.IsEmpty
                    && comp.WallTileSnapshot.TryGetValue(tile, out var savedTile))
                {
                    _map.SetTile(grid.Value, gridComp, tile, savedTile);
                }

                var coords = _map.GridTileToLocal(grid.Value, gridComp, tile);

                // На тайле стоит/лежит существо (например, дуэлянт, упавший в крит на месте снесённой
                // стены)? Отодвигаем его на ближайший свободный тайл, чтобы стена не зажала. Если
                // отодвинуть некуда — стену здесь не ставим (тайл восстановится на следующем раунде,
                // мусор уже убран выше).
                var blocked = false;
                foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(coords, 0.45f))
                {
                    if (TryFindFreeTile(grid.Value, gridComp, comp, tile, out var freeCoords))
                        _transform.SetCoordinates(mob.Owner, freeCoords);
                    else
                        blocked = true;
                }

                if (blocked)
                {
                    blockedCount++;
                    continue;
                }

                var newWall = Spawn(proto, coords);
                if (_transform.AnchorEntity(newWall))
                {
                    respawned++;
                }
                else
                {
                    failed++;
                    Log.Warning($"[duel-arena] не удалось заякорить восстановленную стену {proto} на тайле {tile}");
                }
            }
            catch (Exception e)
            {
                failed++;
                Log.Error($"[duel-arena] ошибка восстановления стены на тайле {tile}: {e}");
            }
        }

        Log.Info($"[duel-arena] восстановление стен ({ToPrettyString(arenaUid)}): снимок {comp.WallSnapshot.Count}, "
            + $"вылечено {healed}, переставлено {respawned}, занято мобами {blockedCount}, ошибок {failed}");
    }

    /// <summary>
    /// Остаток снесённой стены — балка (Girder/ReinforcedGirder/ClockworkGirder/BrassGirder и т.п.).
    /// Только такие сущности убираем перед восстановлением стены; настенные предметы не трогаем.
    /// </summary>
    private bool IsWallDebris(EntityUid uid)
    {
        // Сущность из списка заякоренных могла быть уже удалена (например, каскадом от Del
        // соседнего мусора или отложенным QueueDel клинапа) — MetaData на ней кидает исключение.
        return Exists(uid) && MetaData(uid).EntityPrototype?.ID is { } proto && proto.Contains("Girder");
    }

    /// <summary>
    /// Ищет ближайший свободный тайл вокруг <paramref name="origin"/> (кольцами наружу, в пределах
    /// нескольких клеток), куда можно отодвинуть тело, чтобы поставить стену. Свободный тайл —
    /// тот, где нет стены и где стена не вырастет (отсутствует в снимке).
    /// </summary>
    private bool TryFindFreeTile(EntityUid gridUid, MapGridComponent gridComp, DuelArenaComponent comp, Vector2i origin, out EntityCoordinates coords)
    {
        coords = default;
        var check = new List<EntityUid>();

        for (var r = 1; r <= 4; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    // Только периметр текущего кольца (чебышёвское расстояние == r).
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                        continue;

                    var candidate = origin + new Vector2i(dx, dy);

                    // Пропускаем тайлы, где стена уже есть или вырастет по снимку.
                    if (comp.WallSnapshot.ContainsKey(candidate))
                        continue;

                    check.Clear();
                    _map.GetAnchoredEntities((gridUid, gridComp), candidate, check);
                    if (check.Any(e => _tag.HasTag(e, WallTag)))
                        continue;

                    coords = _map.GridTileToLocal(gridUid, gridComp, candidate);
                    return true;
                }
            }
        }

        return false;
    }
}
