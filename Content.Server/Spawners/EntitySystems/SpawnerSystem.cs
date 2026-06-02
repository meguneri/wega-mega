using Content.Server.Spawners.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Spawners.EntitySystems;

public sealed partial class SpawnerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    /// <summary>
    /// How many times to retry finding an unblocked tile before giving up and using the base position.
    /// </summary>
    private const int MaxSpawnAttempts = 10;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimedSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<TimedSpawnerComponent>();
        while (query.MoveNext(out var uid, out var timedSpawner))
        {
            if (!timedSpawner.Enabled)
                continue;

            if (timedSpawner.NextFire > curTime)
                continue;

            OnTimerFired(uid, timedSpawner);

            timedSpawner.NextFire += timedSpawner.IntervalSeconds;
        }
    }

    private void OnMapInit(Entity<TimedSpawnerComponent> ent, ref MapInitEvent args)
    {
        // Always set NextFire so that if Enabled is toggled on later,
        // the NextFire timestamp will be recalculated at that moment.
        // Disabled spawners will have NextFire reset when they are enabled.
        ent.Comp.NextFire = _timing.CurTime + ent.Comp.IntervalSeconds;
    }

    /// <summary>
    /// Enables or disables this spawner. When enabling, resets the timer so the
    /// first spawn happens after a full interval (not immediately).
    /// </summary>
    public void SetEnabled(EntityUid uid, TimedSpawnerComponent comp, bool enabled)
    {
        comp.Enabled = enabled;
        if (enabled)
        {
            // Reset NextFire so first spawn is one full interval from now.
            comp.NextFire = _timing.CurTime + comp.IntervalSeconds;
        }
    }

    private void OnTimerFired(EntityUid uid, TimedSpawnerComponent component)
    {
        if (!_random.Prob(component.Chance))
            return;

        var number = _random.Next(component.MinimumEntitiesSpawned, component.MaximumEntitiesSpawned);

        // Determine base spawn position: prefer SpawnNearEntity if set and still valid.
        var refUid = component.SpawnNearEntity is { } nearUid && Exists(nearUid) ? nearUid : uid;
        var baseCoords = Transform(refUid).Coordinates;

        for (var i = 0; i < number; i++)
        {
            var entity = _random.Pick(component.Prototypes);
            var spawnCoords = component.RandomSpawnRadius > 0f
                ? FindUnblockedCoords(baseCoords, component.RandomSpawnRadius)
                : baseCoords;

            try
            {
                SpawnAtPosition(entity, spawnCoords);
            }
            catch (Exception e)
            {
                Log.Error($"TimedSpawner: failed to spawn '{entity}' at {spawnCoords}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Picks a random position within <paramref name="radius"/> of <paramref name="origin"/> that
    /// is not inside a wall or space tile. Falls back to <paramref name="origin"/> if no valid
    /// position is found within <see cref="MaxSpawnAttempts"/> tries.
    /// </summary>
    private EntityCoordinates FindUnblockedCoords(EntityCoordinates origin, float radius)
    {
        for (var attempt = 0; attempt < MaxSpawnAttempts; attempt++)
        {
            var candidate = origin.Offset(_random.NextVector2(radius));

            if (_turf.TryGetTileRef(candidate, out var tileRef))
            {
                // Skip space tiles and tiles blocked by walls/impassable fixtures.
                if (_turf.IsSpace(tileRef.Value))
                    continue;

                if (_turf.IsTileBlocked(tileRef.Value, CollisionGroup.Impassable))
                    continue;
            }
            else
            {
                // No tile at this position (off-grid) — skip.
                continue;
            }

            return candidate;
        }

        // All attempts failed — fall back to the origin.
        return origin;
    }
}
