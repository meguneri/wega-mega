using Content.Server._Wega.Raid.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Server._Wega.Raid.Systems;

/// <summary>
/// Раскладывает добычу рейда: при инициализации карты каждый <see cref="RaidLootSpawnerComponent"/>
/// с заданным шансом спавнит случайные предметы, помечает их меткой <see cref="RaidLootComponent"/>
/// (рекурсивно по содержимому) и самоудаляется. Метка нужна, чтобы при экстракте посчитать и
/// «продать» именно добытый лут, не трогая стартовое снаряжение игрока.
/// </summary>
public sealed partial class RaidLootSpawnerSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RaidLootSpawnerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RaidLootSpawnerComponent comp, MapInitEvent args)
    {
        if (comp.Loot.Count == 0)
        {
            QueueDel(uid);
            return;
        }

        if (_random.NextFloat() <= comp.SpawnChance)
        {
            var max = Math.Max(comp.MinItems, comp.MaxItems);
            var count = _random.Next(comp.MinItems, max + 1);
            var coords = Transform(uid).Coordinates;

            for (var i = 0; i < count; i++)
            {
                var item = Spawn(_random.Pick(comp.Loot), coords);
                MarkLootRecursive(item);
            }
        }

        // Одноразовый: убираем спавнер, чтобы он не висел на карте.
        QueueDel(uid);
    }

    /// <summary>Метит предмет и всё его содержимое как добычу рейда. Живых существ не трогает.</summary>
    public void MarkLootRecursive(EntityUid uid)
    {
        if (HasComp<MobStateComponent>(uid))
            return;

        EnsureComp<RaidLootComponent>(uid);

        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return;

        foreach (var c in _container.GetAllContainers(uid, manager))
            foreach (var contained in c.ContainedEntities)
                MarkLootRecursive(contained);
    }
}
