using System.Linq;
using Content.Shared._Wega.Raid.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Raid.Systems;

/// <summary>
/// Авто-перезарядка скавов (<see cref="RaidSkavAutoReloadComponent"/>): когда держимый магазинный
/// ствол пуст, скав вынимает пустой магазин и вставляет совместимый заряженный из своего инвентаря
/// (карманы/сумка/вторая рука), с короткой паузой. Запасные кончились — ствол остаётся пустым, и HTN
/// уводит скава в ближний бой.
///
/// Боезапас определяется через <see cref="GetAmmoCountEvent"/> (не через закрытые компоненты), смена
/// магазина — через контейнер слота <c>gun_magazine</c>, на изменение которого сам ствол реагирует
/// и пересчитывает патроны.
/// </summary>
public sealed partial class RaidSkavAutoReloadSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    private const string MagSlot = "gun_magazine";

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RaidSkavAutoReloadComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextCheck)
                continue;
            comp.NextCheck = now + TimeSpan.FromSeconds(comp.CheckInterval);

            if (_mobState.IsAlive(uid))
                TryReload((uid, comp), now);
        }
    }

    private void TryReload(Entity<RaidSkavAutoReloadComponent> skav, TimeSpan now)
    {
        // Держимый магазинный ствол.
        EntityUid? gun = null;
        foreach (var held in _hands.EnumerateHeld(skav.Owner))
        {
            if (HasComp<GunComponent>(held) && _container.TryGetContainer(held, MagSlot, out _))
            {
                gun = held;
                break;
            }
        }

        if (gun is not { } gunUid)
            return;

        // Не пуст — перезаряжать нечего.
        if (GetAmmo(gunUid) > 0)
            return;

        if (!_container.TryGetContainer(gunUid, MagSlot, out var magContainer))
            return;

        // Запасной заряженный магазин в инвентаре (не сам ствол, не другое оружие).
        if (FindSpareMag(skav.Owner, gunUid) is not { } spareMag)
            return;

        // Вынуть и выбросить пустой магазин (если он есть в слоте), затем вставить заряженный.
        foreach (var old in magContainer.ContainedEntities.ToList())
        {
            _container.Remove(old, magContainer);
            QueueDel(old);
        }

        if (_container.Insert(spareMag, magContainer))
            skav.Comp.NextCheck = now + TimeSpan.FromSeconds(skav.Comp.ReloadDelay);
    }

    /// <summary>Доступный боезапас сущности (ствола или магазина) через событие — без доступа к компонентам.</summary>
    private int GetAmmo(EntityUid uid)
    {
        var ev = new GetAmmoCountEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Count;
    }

    /// <summary>Первый заряженный магазин в инвентаре скава (не ствол и не другое оружие).</summary>
    private EntityUid? FindSpareMag(EntityUid skav, EntityUid gun)
    {
        var candidates = new List<EntityUid>();
        CollectItems(skav, gun, candidates);

        foreach (var c in candidates)
        {
            if (!HasComp<GunComponent>(c) && GetAmmo(c) > 0)
                return c;
        }
        return null;
    }

    /// <summary>Рекурсивно собирает предметы из дерева трансформа (надетое/в руках/в сумках), кроме ствола и живых.</summary>
    private void CollectItems(EntityUid root, EntityUid gun, List<EntityUid> into)
    {
        var en = Transform(root).ChildEnumerator;
        while (en.MoveNext(out var child))
        {
            if (child == gun || HasComp<MobStateComponent>(child))
                continue;

            into.Add(child);
            CollectItems(child, gun, into);
        }
    }
}
