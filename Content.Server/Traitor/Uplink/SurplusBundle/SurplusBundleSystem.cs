using System;
using System.Linq;
using Content.Server._Wega.Duel.Components;
using Content.Server._Wega.Duel.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Server.Store.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Random;

namespace Content.Server.Traitor.Uplink.SurplusBundle;

public sealed class SurplusBundleSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly DuelArenaCleanupSystem _cleanup = default!;

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

        // Tag arena-issued gear so the duel cleanup only removes what the crate gave out.
        // MarkIssuedItems is set exclusively on the dedicated duel arena crates, which are
        // bought from the uplink BEFORE the duelists arm the fight — so we tag on fill rather
        // than gating on an active duel (the crate is filled while the duel isn't running yet).
        var shouldTag = ent.Comp1.MarkIssuedItems;

        foreach (var item in content)
        {
            var dode = Spawn(item.ProductEntity, cords);

            if (shouldTag)
                _cleanup.MarkIssuedRecursive(dode);

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
