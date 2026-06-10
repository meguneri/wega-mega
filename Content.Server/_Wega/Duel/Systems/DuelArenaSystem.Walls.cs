using System.Linq;
using Content.Server._Wega.Duel.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Восстановление стен дуэльной арены. Перед первой дуэлью снимается пристайн-снимок стен
/// (тайл → прототип). После каждой дуэли разрушенные за бой стены восстанавливаются по снимку:
/// на тайлах, где стены не осталось, убирается мусор (балки/обломки) и заново спавнится стена.
/// </summary>
public sealed partial class DuelArenaSystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    /// <summary>
    /// Снимает пристайн-планировку стен арены один раз (при старте первой дуэли, пока стены целы).
    /// </summary>
    private void EnsureWallSnapshot(EntityUid arenaUid, DuelArenaComponent comp)
    {
        if (comp.WallSnapshotTaken)
            return;

        comp.WallSnapshotTaken = true;

        var grid = Transform(arenaUid).GridUid;
        if (grid == null || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        // Проходим по всем тегированным сущностям и берём только стены на гриде арены.
        var query = EntityQueryEnumerator<TagComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var tagComp, out var xform))
        {
            if (xform.GridUid != grid || !_tag.HasTag(tagComp, WallTag))
                continue;

            if (MetaData(uid).EntityPrototype?.ID is not { } proto)
                continue;

            var tile = _map.TileIndicesFor(grid.Value, gridComp, xform.Coordinates);
            comp.WallSnapshot[tile] = proto;
        }
    }

    /// <summary>
    /// Восстанавливает стены, разрушенные за прошедшую дуэль, по снимку <see cref="DuelArenaComponent.WallSnapshot"/>.
    /// </summary>
    private void RestoreWalls(EntityUid arenaUid, DuelArenaComponent comp)
    {
        if (comp.WallSnapshot.Count == 0)
            return;

        var grid = Transform(arenaUid).GridUid;
        if (grid == null || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var anchored = new List<EntityUid>();
        foreach (var (tile, proto) in comp.WallSnapshot)
        {
            anchored.Clear();
            _map.GetAnchoredEntities((grid.Value, gridComp), tile, anchored);

            // Стена на месте — ничего не делаем.
            if (anchored.Any(e => _tag.HasTag(e, WallTag)))
                continue;

            var coords = _map.GridTileToLocal(grid.Value, gridComp, tile);

            // На тайле стоит/лежит существо (например, дуэлянт, упавший в крит на месте снесённой
            // стены)? Отодвигаем его на ближайший свободный тайл, чтобы стена не зажала. Если
            // отодвинуть некуда — стену здесь не ставим (тайл восстановится на следующей дуэли).
            var blocked = false;
            foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(coords, 0.45f))
            {
                if (TryFindFreeTile(grid.Value, gridComp, comp, tile, out var freeCoords))
                    _transform.SetCoordinates(mob.Owner, freeCoords);
                else
                    blocked = true;
            }

            if (blocked)
                continue;

            // Освобождаем тайл от обломков (балки и т.п.), затем ставим стену заново.
            foreach (var debris in anchored)
                Del(debris);

            var wall = Spawn(proto, coords);
            _transform.AnchorEntity(wall);
        }
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
