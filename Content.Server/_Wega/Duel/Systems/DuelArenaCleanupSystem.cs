using Content.Server._Wega.Duel.Components;
using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Modular.Suit;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Shared.Modular.Suit;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Fluids.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Modular.Suit;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Очистка дуэльной арены. Удаляет:
/// — предметы из ящика-арсенала (<see cref="ArenaIssuedItemComponent"/>),
/// — потраченные гильзы (<see cref="CartridgeAmmoComponent"/> с Spent=true),
/// — лужи крови/химии.
/// Замаппленные вещи карты не трогаются: они загружаются до старта дуэли
/// и поэтому не получают тег ArenaIssuedItem.
/// </summary>
public sealed class DuelArenaCleanupSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ModularSuitSystem _modSuit = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DuelArenaCleanupComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DuelArenaCleanupComponent, SignalReceivedEvent>(OnSignalReceived);

        // Тегаем гильзы и прочие картриджи, заспавненные во время активной дуэли,
        // чтобы клинап их убрал независимо от того, из чьего оружия они вылетели.
        SubscribeLocalEvent<CartridgeAmmoComponent, ComponentStartup>(OnCartridgeStartup);

        // МОД-скафандры: модули спавнятся в MapInitEvent уже ПОСЛЕ того, как MarkIssuedRecursive
        // отработал на самом скафандре. Подписываемся после ModularSuitSystem, чтобы модули
        // уже были вставлены в контейнер, и тегируем их если скафандр помечен.
        SubscribeLocalEvent<ModularSuitPreassembledComponent, MapInitEvent>(
            OnModSuitMapInit, after: [typeof(ModularSuitSystem)]);

        // Хардсьюты и другая одежда с переключаемым шлемом: ToggleableClothingSystem спавнит
        // шлем (ClothingUid) в MapInitEvent. Тегируем шлем если сам скафандр помечен.
        SubscribeLocalEvent<ToggleableClothingComponent, MapInitEvent>(
            OnToggleableClothingMapInit, after: [typeof(ToggleableClothingSystem)]);

        // Аирдроп-ящик снаряжения: StorageFill кладёт лут в MapInitEvent. Подписываемся
        // после StorageSystem, чтобы содержимое уже было в ящике, и помечаем ящик + весь
        // лут рекурсивно — тогда очистка арены уберёт их после раунда.
        SubscribeLocalEvent<ArenaSupplyDropComponent, MapInitEvent>(
            OnSupplyDropMapInit, after: [typeof(StorageSystem)]);
    }

    private void OnSupplyDropMapInit(EntityUid uid, ArenaSupplyDropComponent comp, MapInitEvent args)
    {
        MarkIssuedRecursive(uid);
    }

    private void OnInit(EntityUid uid, DuelArenaCleanupComponent comp, ComponentInit args)
    {
        _link.EnsureSinkPorts(uid, comp.TriggerPort);
    }

    private void OnCartridgeStartup(EntityUid uid, CartridgeAmmoComponent comp, ComponentStartup args)
    {
        if (IsDuelActive())
            EnsureComp<ArenaIssuedItemComponent>(uid);
    }

    private void OnToggleableClothingMapInit(EntityUid uid, ToggleableClothingComponent comp, MapInitEvent args)
    {
        if (!HasComp<ArenaIssuedItemComponent>(uid))
            return;

        if (comp.ClothingUid != null)
            MarkIssuedRecursive(comp.ClothingUid.Value);
    }

    /// <summary>
    /// Тегирует сущность и всё вложенное в её контейнеры тегом <see cref="ArenaIssuedItemComponent"/>.
    /// Используется при спавне предметов из арена-ящика и кубика войны.
    /// </summary>
    public void MarkIssuedRecursive(EntityUid uid)
    {
        EnsureComp<ArenaIssuedItemComponent>(uid);

        // Пристёгнутая одежда (шлем хардсьюта и т.п.): её сущность (ClothingUid) при
        // развёрнутом состоянии лежит не в контейнере, а надета в слот — рекурсия по
        // контейнерам её не достанет. Тегируем явно, иначе шлем переживёт очистку.
        if (TryComp<ToggleableClothingComponent>(uid, out var toggleable) && toggleable.ClothingUid != null)
            MarkIssuedRecursive(toggleable.ClothingUid.Value);

        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return;

        foreach (var c in _container.GetAllContainers(uid, manager))
            foreach (var contained in c.ContainedEntities)
                MarkIssuedRecursive(contained);
    }

    private void OnModSuitMapInit(EntityUid uid, ModularSuitPreassembledComponent comp, MapInitEvent args)
    {
        // Если скафандр не помечен как выданный аренной — не трогаем.
        if (!HasComp<ArenaIssuedItemComponent>(uid))
            return;

        // Тегируем все вложенные сущности (модули, батарея, части брони).
        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return;

        foreach (var container in _container.GetAllContainers(uid, manager))
        {
            foreach (var contained in container.ContainedEntities)
                EnsureComp<ArenaIssuedItemComponent>(contained);
        }
    }

    private void OnSignalReceived(EntityUid uid, DuelArenaCleanupComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.TriggerPort)
            return;

        if (IsDuelActiveNearby(uid, comp.Range))
            return;

        CleanupArea(uid, comp.Range);
        _chat.DispatchServerAnnouncement("Арена очищена: выданное снаряжение убрано.", Color.Gold);
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

    private bool IsDuelActiveNearby(EntityUid originEntity, float range)
    {
        var origin = Transform(originEntity).MapPosition;
        var query = EntityQueryEnumerator<DuelArenaComponent>();
        while (query.MoveNext(out var arenaUid, out var arena))
        {
            if (!arena.IsActive)
                continue;
            if (InRange(arenaUid, origin, range))
                return true;
        }
        return false;
    }

    public void CleanupArea(EntityUid originEntity, float range)
    {
        var origin = Transform(originEntity).MapPosition;

        // 1. Снаряжение из ящика + гильзы (все помечены ArenaIssuedItemComponent).
        var issuedQuery = EntityQueryEnumerator<ArenaIssuedItemComponent>();
        while (issuedQuery.MoveNext(out var itemUid, out _))
        {
            if (!InRange(itemUid, origin, range))
                continue;

            if (Transform(itemUid).Anchored)
                continue;

            // Вколотый имплант: принудительно вынимаем, чтобы SharedSubdermalImplantSystem
            // корректно снял дарованные действия/компоненты.
            if (_container.TryGetContainingContainer((itemUid, null), out var container)
                && container.ID == ImplanterComponent.ImplantSlotId)
            {
                _container.Remove(itemUid, container, reparent: false, force: true);
            }

            // МОД-контроллер: его развёрнутые части надеты в слоты брони игрока, а не вложены
            // в контроллер — удаляем их явно, иначе шлем/нагрудник/перчатки/ботинки останутся
            // на игроке после удаления самого МОД.
            if (HasComp<ModularSuitComponent>(itemUid))
            {
                foreach (var part in _modSuit.GetEquippedParts(itemUid))
                    QueueDel(part);
            }

            QueueDel(itemUid);
        }

        // 2. Лужи на полу (кровь, химия и т.п.).
        var puddleQuery = EntityQueryEnumerator<PuddleComponent>();
        while (puddleQuery.MoveNext(out var puddleUid, out _))
        {
            if (!InRange(puddleUid, origin, range))
                continue;

            QueueDel(puddleUid);
        }
    }

    private bool InRange(EntityUid target, MapCoordinates origin, float range)
    {
        var pos = Transform(target).MapPosition;
        if (pos.MapId != origin.MapId)
            return false;
        return (pos.Position - origin.Position).Length() <= range;
    }
}
