using System.Linq;
using Content.Server._Wega.Duel.Components;
using Content.Server.Light.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Light.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Восстановление стен и окон дуэльной арены. При каждом старте дуэли (конструкции целы) планировка
/// стен и окон мержится в снимок (тайл → прототип + тайл пола под ним). После каждого раунда
/// конструкции приводятся к снимку: целая правильная стена/окно чинится (помятая/треснувшее
/// становится новым); отсутствующая / разрушенная до балки / чужая конструкция убирается и заново
/// ставится свежей; уничтоженный пол под ней восстанавливается. Восстановление выполняется отложенно
/// (см. PendingWallRestore) — на тике после завершения боя, вне стека события смерти.
/// </summary>
public sealed partial class DuelArenaSystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private PoweredLightSystem _poweredLight = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";
    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

    /// <summary>
    /// Восстанавливаемая конструкция арены — стена (тег <see cref="WallTag"/>) или окно
    /// (тег <see cref="WindowTag"/>). И то, и другое попадает в снимок и чинится/переставляется
    /// по одинаковой логике.
    /// </summary>
    private bool IsRestorableStructure(EntityUid uid)
        => _tag.HasTag(uid, WallTag) || _tag.HasTag(uid, WindowTag);

    private bool IsRestorableStructure(TagComponent tagComp)
        => _tag.HasTag(tagComp, WallTag) || _tag.HasTag(tagComp, WindowTag);

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
            if (xform.GridUid != grid || !IsRestorableStructure(tagComp))
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

                // Ищем стену/окно на тайле.
                EntityUid? wall = null;
                foreach (var e in anchored)
                {
                    if (Exists(e) && IsRestorableStructure(e))
                    {
                        wall = e;
                        break;
                    }
                }

                // Правильная стена/окно уже стоит — просто чиним повреждения (помятая → новая) и идём дальше.
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
                    if (Exists(debris) && (IsWallDebris(debris) || IsRestorableStructure(debris)))
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
                    if (check.Any(IsRestorableStructure))
                        continue;

                    coords = _map.GridTileToLocal(gridUid, gridComp, candidate);
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Мержит текущую расстановку светильников арены в снимок (тайл → прототип + поворот).
    /// Вызывается при КАЖДОМ старте дуэли по той же логике, что и снимок стен: новые светильники
    /// добавляются, уже записанные не перезаписываются. Берёт любые сущности с
    /// <see cref="PoweredLightComponent"/> на гриде арены.
    /// </summary>
    private void SnapshotLights(EntityUid arenaUid, DuelArenaComponent comp)
    {
        var grid = Transform(arenaUid).GridUid;
        if (grid == null || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var added = 0;

        var query = EntityQueryEnumerator<PoweredLightComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            if (MetaData(uid).EntityPrototype?.ID is not { } proto)
                continue;

            var tile = _map.TileIndicesFor(grid.Value, gridComp, xform.Coordinates);
            if (comp.LightSnapshot.TryAdd(tile, proto))
            {
                comp.LightRotationSnapshot[tile] = xform.LocalRotation;
                added++;
            }
        }

        if (added > 0)
            Log.Info($"[duel-arena] снимок светильников пополнен: +{added}, всего {comp.LightSnapshot.Count} тайлов");
    }

    /// <summary>
    /// Приводит светильники арены к снимку <see cref="DuelArenaComponent.LightSnapshot"/>. Для каждого
    /// тайла: если светильник нужного типа на месте — чиним корпус (помятый → как новый) и лампу
    /// (разбитую/перегоревшую/отсутствующую меняем на свежую); если светильник уничтожен целиком —
    /// ставим новый с тем же поворотом. Каждый тайл обрабатывается независимо. Вызывается из Update
    /// на тике после завершения боя, рядом с восстановлением стен.
    /// </summary>
    private void RestoreLights(EntityUid arenaUid, DuelArenaComponent comp)
    {
        if (comp.LightSnapshot.Count == 0)
            return;

        var grid = Transform(arenaUid).GridUid;
        if (grid == null || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var healed = 0;
        var respawned = 0;
        var failed = 0;

        var anchored = new List<EntityUid>();
        foreach (var (tile, proto) in comp.LightSnapshot)
        {
            try
            {
                anchored.Clear();
                _map.GetAnchoredEntities((grid.Value, gridComp), tile, anchored);

                // Светильник нужного типа на тайле?
                EntityUid? fixture = null;
                foreach (var e in anchored)
                {
                    if (Exists(e) && HasComp<PoweredLightComponent>(e) && MetaData(e).EntityPrototype?.ID == proto.Id)
                    {
                        fixture = e;
                        break;
                    }
                }

                // Светильник уцелел — чиним корпус и лампу на месте (сохраняя поворот/проводку).
                if (fixture is { } light)
                {
                    if (TryComp<DamageableComponent>(light, out var dmg))
                        _damageable.SetAllDamage((light, dmg), FixedPoint2.Zero);

                    RestoreBulb(light);
                    healed++;
                    continue;
                }

                // Светильник уничтожен — убираем обломки своего типа и ставим свежий.
                foreach (var debris in anchored)
                {
                    if (Exists(debris) && HasComp<PoweredLightComponent>(debris))
                        Del(debris);
                }

                var coords = _map.GridTileToLocal(grid.Value, gridComp, tile);
                var newLight = Spawn(proto, coords);

                if (comp.LightRotationSnapshot.TryGetValue(tile, out var rot))
                    _transform.SetLocalRotation(newLight, rot);

                if (_transform.AnchorEntity(newLight))
                {
                    respawned++;
                }
                else
                {
                    failed++;
                    Log.Warning($"[duel-arena] не удалось заякорить восстановленный светильник {proto} на тайле {tile}");
                }
            }
            catch (Exception e)
            {
                failed++;
                Log.Error($"[duel-arena] ошибка восстановления светильника на тайле {tile}: {e}");
            }
        }

        Log.Info($"[duel-arena] восстановление светильников ({ToPrettyString(arenaUid)}): снимок {comp.LightSnapshot.Count}, "
            + $"вылечено {healed}, переставлено {respawned}, ошибок {failed}");
    }

    /// <summary>
    /// Возвращает лампу светильника в рабочее состояние: целую и горящую не трогает, а разбитую,
    /// перегоревшую или отсутствующую заменяет свежей (по <see cref="PoweredLightComponent.HasLampOnSpawn"/>).
    /// </summary>
    private void RestoreBulb(EntityUid light)
    {
        if (!TryComp<PoweredLightComponent>(light, out var comp))
            return;

        var bulb = _poweredLight.GetBulb(light, comp);

        // Лампа на месте и цела — ничего не делаем.
        if (bulb is { } present && TryComp<LightBulbComponent>(present, out var bulbComp) && bulbComp.State == LightBulbState.Normal)
            return;

        // Нечем заменить (у светильника не задана лампа по умолчанию) — оставляем как есть.
        if (comp.HasLampOnSpawn is not { } lampProto)
            return;

        // Удаляем разбитую/перегоревшую лампу (Del, а не выброс — чтобы не сорить осколками в арене).
        if (bulb is { } old)
            Del(old);

        var fresh = Spawn(lampProto, Transform(light).Coordinates);
        _poweredLight.InsertBulb(light, fresh, comp);
    }
}
