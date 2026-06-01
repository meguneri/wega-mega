using System;
using System.Linq;
using Content.Server._Wega.Duel.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Uplink.SurplusBundle;

public sealed class SurplusBundleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

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

        // Only tag items when a duel is actually running. Crates placed on the map at load time
        // must not be tagged — otherwise cleanup after a later duel would delete their contents.
        var shouldTag = ent.Comp1.MarkIssuedItems && IsDuelActive();

        foreach (var item in content)
        {
            var dode = Spawn(item.ProductEntity, cords);

            if (shouldTag)
                MarkIssuedRecursive(dode);

            _entityStorage.Insert(dode, ent);
        }
    }

    private bool IsDuelActive()
    {
        var query = EntityQueryEnumerator<DuelArenaComponent>();
        while (query.MoveNext(out _, out var arena))
        {
            if (arena.IsActive)
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Tags an entity and everything nested in its containers with <see cref="ArenaIssuedItemComponent"/>.
    /// </summary>
    private void MarkIssuedRecursive(EntityUid uid)
    {
        EnsureComp<ArenaIssuedItemComponent>(uid);

        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return;

        foreach (var container in _container.GetAllContainers(uid, manager))
        {
            foreach (var contained in container.ContainedEntities)
                MarkIssuedRecursive(contained);
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

        // Guaranteed picks: ensure at least one item from each guaranteed category (budget permitting).
        foreach (var category in ent.Comp1.GuaranteedCategories)
        {
            if (ret.Count >= ent.Comp1.MaxItems)
                break;

            var remainingBudget = ent.Comp1.TotalPrice - totalCost;

            var eligible = listings
                .Where(l => l.Categories.Any(c => c.Id == category) &&
                            l.Cost.Values.Sum() <= remainingBudget &&
                            !ExceedsCategoryLimit(l, ent.Comp1.CategoryLimits, categoryCounts))
                .ToList();

            if (eligible.Count == 0)
                continue;

            var guaranteed = PickItem(eligible, ent.Comp1);
            ret.Add(guaranteed);
            totalCost += guaranteed.Cost.Values.Sum();

            foreach (var cat in guaranteed.Categories)
                categoryCounts[cat.Id] = categoryCounts.GetValueOrDefault(cat.Id) + 1;

            listings.Remove(guaranteed);
        }

        while (totalCost < ent.Comp1.TotalPrice && ret.Count < ent.Comp1.MaxItems)
        {
            var remainingBudget = ent.Comp1.TotalPrice - totalCost;

            // Eligible: within budget and not over any category limit
            var eligible = listings
                .Where(l => l.Cost.Values.Sum() <= remainingBudget &&
                            !ExceedsCategoryLimit(l, ent.Comp1.CategoryLimits, categoryCounts))
                .ToList();

            if (eligible.Count == 0)
                break;

            var pick = PickItem(eligible, ent.Comp1);
            ret.Add(pick);
            totalCost += pick.Cost.Values.Sum();

            foreach (var cat in pick.Categories)
                categoryCounts[cat.Id] = categoryCounts.GetValueOrDefault(cat.Id) + 1;

            listings.Remove(pick);
        }

        return ret;
    }

    /// <summary>
    ///     Picks one listing from the eligible set. Uniform by default; if the bundle has
    ///     <see cref="SurplusBundleComponent.WeightByCost"/> set, probability is weighted by
    ///     price^<see cref="SurplusBundleComponent.CostWeightExponent"/> so pricier gear is more likely
    ///     (gently, by default), while top-tier items stay a rare jackpot.
    /// </summary>
    private ListingDataWithCostModifiers PickItem(List<ListingDataWithCostModifiers> eligible, SurplusBundleComponent comp)
    {
        if (!comp.WeightByCost)
            return eligible[_random.Next(0, eligible.Count)];

        var exponent = comp.CostWeightExponent;

        var total = 0.0;
        foreach (var listing in eligible)
            total += Weight(listing, exponent);

        var roll = _random.NextDouble() * total;
        foreach (var listing in eligible)
        {
            roll -= Weight(listing, exponent);
            if (roll <= 0.0)
                return listing;
        }

        return eligible[^1];
    }

    private static double Weight(ListingDataWithCostModifiers listing, double exponent)
    {
        return Math.Pow(Math.Max(1.0, listing.Cost.Values.Sum().Double()), exponent);
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
