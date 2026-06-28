using Content.Server._Wega.Raid.Components;
using Content.Shared._Wega.Raid.Components;
using Content.Shared.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Server._Wega.Raid.Systems;

/// <summary>
/// При смерти носителя добычи (<see cref="RaidLootCarrierComponent"/> — скавы/боссы) метит всё его
/// снаряжение как добычу рейда (<see cref="RaidLootComponent"/>), чтобы игрок мог вынести его за
/// деньги, и спавнит гарантированный бонус-дроп боссов у трупа.
/// </summary>
public sealed partial class RaidLootCarrierSystem : EntitySystem
{
    [Dependency] private RaidLootSpawnerSystem _loot = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RaidLootCarrierComponent, MobStateChangedEvent>(OnMobState);
    }

    private void OnMobState(EntityUid uid, RaidLootCarrierComponent comp, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        // Метим всё снаряжение трупа (надетое/в руках/в сумках — это дети трансформа моба).
        // MarkLootRecursive пропускает живых, так что сам моб метку не получит.
        var en = Transform(uid).ChildEnumerator;
        while (en.MoveNext(out var child))
            _loot.MarkLootRecursive(child);

        // Гарантированный бонус-дроп (боссы).
        var coords = Transform(uid).Coordinates;
        foreach (var proto in comp.BonusLoot)
        {
            var item = Spawn(proto, coords);
            _loot.MarkLootRecursive(item);
        }
    }
}
