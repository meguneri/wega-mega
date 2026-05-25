using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Store.Components;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Uplink.SurplusBundle;

public sealed class SurplusBundleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly StoreSystem _store = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurplusBundleComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, SurplusBundleComponent component, MapInitEvent args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        FillStorage((uid, component, store));
    }

    private void FillStorage(Entity<SurplusBundleComponent, StoreComponent> ent)
    {
        var cords = Transform(ent).Coordinates;
        var content = GetRandomContent(ent);
        foreach (var item in content)
        {
            var dode = Spawn(item.ProductEntity, cords);
            _entityStorage.Insert(dode, ent);
        }
    }

    private List<ListingData> GetRandomContent(Entity<SurplusBundleComponent, StoreComponent> ent)
    {
        var ret = new List<ListingData>();
        var categoryCounts = new Dictionary<string, int>();

        var listings = _store.GetAvailableListings(ent, null, ent.Comp2.Categories)
            .OrderBy(p => p.Cost.Values.Sum())
            .ToList();

        if (listings.Count == 0)
            return ret;

        var totalCost = FixedPoint2.Zero;
        while (totalCost < ent.Comp1.TotalPrice)
        {
            var remainingBudget = ent.Comp1.TotalPrice - totalCost;

            // Eligible: within budget and not over any category limit
            var eligible = listings
                .Where(l => l.Cost.Values.Sum() <= remainingBudget &&
                            !ExceedsCategoryLimit(l, ent.Comp1.CategoryLimits, categoryCounts))
                .ToList();

            if (eligible.Count == 0)
                break;

            var pick = eligible[_random.Next(0, eligible.Count)];
            ret.Add(pick);
            totalCost += pick.Cost.Values.Sum();

            foreach (var cat in pick.Categories)
                categoryCounts[cat.Id] = categoryCounts.GetValueOrDefault(cat.Id) + 1;

            listings.Remove(pick);
        }

        return ret;
    }

    private static bool ExceedsCategoryLimit(
        ListingData listing,
        Dictionary<string, int> limits,
        Dictionary<string, int> counts)
    {
        foreach (var cat in listing.Categories)
        {
            if (limits.TryGetValue(cat.Id, out var limit) && counts.GetValueOrDefault(cat.Id) >= limit)
                return true;
        }
        return false;
    }
}
