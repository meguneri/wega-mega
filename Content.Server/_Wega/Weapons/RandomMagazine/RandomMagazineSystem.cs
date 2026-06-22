using Content.Shared._Wega.Weapons.RandomMagazine;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Random;

namespace Content.Server._Wega.Weapons.RandomMagazine;

public sealed partial class RandomMagazineSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RandomMagazineComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RandomMagazineComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Magazines.Count == 0)
            return;

        var proto = _random.Pick(ent.Comp.Magazines);

        if (!_slots.TryGetSlot(ent.Owner, SharedGunSystem.MagazineSlot, out var slot))
            return;

        // Remove whatever startingItem put in there
        if (slot.Item is { } existing)
            QueueDel(existing);

        var mag = Spawn(proto, Transform(ent.Owner).Coordinates);
        _slots.TryInsert(ent.Owner, slot, mag, null);
    }
}
