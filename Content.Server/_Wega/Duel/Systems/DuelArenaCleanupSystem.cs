using System.Linq;
using Content.Server._Wega.Duel.Components;
using Content.Server.Botany.Components;
using Content.Server.Chat.Managers;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Traitor.Uplink.SurplusBundle;
using Content.Shared._Wega.Spawners.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Modular.Suit;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Modular.Suit;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Localization;
using Content.Shared.Fluids.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Slippery;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Очистка дуэльной арены. Удаляет:
/// — предметы из ящика-арсенала (<see cref="ArenaIssuedItemComponent"/>),
/// — потраченные гильзы (<see cref="CartridgeAmmoComponent"/> с Spent=true),
/// — лужи крови/химии.
/// Замаппленные вещи карты не трогаются: они загружаются до старта дуэли
/// и поэтому не получают тег ArenaIssuedItem.
/// </summary>
public sealed partial class DuelArenaCleanupSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _link = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ModularSuitSystem _modSuit = default!;
    [Dependency] private SpawnerSystem _spawner = default!;

    /// <summary>
    /// Прототип, который кидает дуэльный спавнер-аирдроп. Спавнер кидает не сам ящик, а
    /// маяк-телеграф (DuelSupplyDropBeacon); по этому id находим и гасим спавнер в конце боя.
    /// </summary>
    private static readonly EntProtoId DuelSupplyDropProto = "DuelSupplyDropBeacon";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DuelArenaCleanupComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DuelArenaCleanupComponent, SignalReceivedEvent>(OnSignalReceived);

        // Тегаем гильзы и прочие картриджи, заспавненные во время активной дуэли,
        // чтобы клинап их убрал независимо от того, из чьего оружия они вылетели.
        SubscribeLocalEvent<CartridgeAmmoComponent, ComponentStartup>(OnCartridgeStartup);

        // МОД-скафандры и прочее: содержимое (модули, батарея, части брони) спавнится и
        // вкладывается в контейнеры уже ПОСЛЕ того, как MarkIssuedRecursive отработал на самом
        // предмете. Нельзя повторно подписаться на ModularSuitPreassembledComponent/MapInitEvent
        // (этим уже владеет ModularSuitSystem), поэтому ловим само вкладывание в контейнер: если
        // владелец контейнера помечен как выданный аренной — помечаем и вложенную сущность.
        // Этот же обработчик покрывает переключаемый шлем хардсьютов: ToggleableClothingSystem
        // спавнит ClothingUid в MapInitEvent и вкладывает его в контейнер скафандра — вставка
        // помеченного аренной предмета тегирует шлем. Отдельная подписка на
        // ToggleableClothingComponent/MapInitEvent не нужна (и запрещена — ею владеет
        // ToggleableClothingSystem).
        SubscribeLocalEvent<ArenaIssuedItemComponent, EntInsertedIntoContainerMessage>(OnIssuedInsert);

        // Аирдроп-ящик снаряжения: StorageFill кладёт лут в MapInitEvent. Подписываемся
        // после StorageSystem, чтобы содержимое уже было в ящике, и помечаем ящик + весь
        // лут рекурсивно — тогда очистка арены уберёт их после раунда.
        SubscribeLocalEvent<ArenaSupplyDropComponent, MapInitEvent>(
            OnSupplyDropMapInit, after: [typeof(StorageSystem)]);

        // Упаковки со SpawnItemsOnUse (набор курильщика Интердайн, коробка гипопена и т.п.)
        // при использовании спавнят НОВОЕ содержимое без метки арены, а сама упаковка
        // самоудаляется. Если упаковка была выдана ареной — пробрасываем метку на содержимое
        // рекурсивно, иначе пачка/зажигалка/гипопен (и сигарета во рту) остаются после боя.
        SubscribeLocalEvent<ArenaIssuedItemComponent, SpawnItemsOnUsedEvent>(OnIssuedSpawnedItems);
    }

    private void OnIssuedSpawnedItems(EntityUid uid, ArenaIssuedItemComponent comp, SpawnItemsOnUsedEvent args)
    {
        foreach (var spawned in args.Spawned)
            MarkIssuedRecursive(spawned);
    }

    /// <summary>
    /// Извлекает из контейнеров предмета всех существ (мобов) перед его удалением, чтобы
    /// каскадный <see cref="QueueDel"/> контейнера не удалил тело вместе с предметом
    /// (например, соперника, спрятавшегося в коробке-невидимке <c>StealthBox</c>).
    /// </summary>
    private void EjectMobsBeforeDelete(EntityUid uid)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return;

        foreach (var container in _container.GetAllContainers(uid, manager))
        {
            // ToList: извлечение мобов модифицирует контейнер во время обхода.
            foreach (var contained in container.ContainedEntities.ToList())
            {
                if (HasComp<MobStateComponent>(contained))
                    // reparent — моб «выпадает» из коробки на её место, а не удаляется с ней.
                    _container.Remove(contained, container, force: true);
                else
                    // Моб мог быть вложен глубже (контейнер в контейнере) — проверяем рекурсивно.
                    EjectMobsBeforeDelete(contained);
            }
        }
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
        // Тегаем гильзу только если она появилась в радиусе активной арены — иначе тег
        // повесился бы на каждый картридж, заспавненный где угодно на сервере во время боя
        // (перезарядка/стрельба вне арены), и чужие гильзы могла бы удалить очистка.
        if (IsInActiveDuelRange(uid))
            EnsureComp<ArenaIssuedItemComponent>(uid);
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

    private void OnIssuedInsert(EntityUid uid, ArenaIssuedItemComponent comp, EntInsertedIntoContainerMessage args)
    {
        // Сущность вложили в контейнер предмета, который сам помечен как выданный аренной
        // (например, модуль в МОД-скафандр при пре-сборке). Помечаем вложенное рекурсивно,
        // чтобы очистка арены убрала и его.
        MarkIssuedRecursive(args.Entity);
    }

    private void OnSignalReceived(EntityUid uid, DuelArenaCleanupComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.TriggerPort)
            return;

        if (IsDuelActiveNearby(uid, comp.Range))
            return;

        CleanupArea(uid, comp.Range);
        _chat.DispatchServerAnnouncement(Loc.GetString("duel-arena-cleaned"), Color.Gold);
    }

    /// <summary>
    /// Находится ли сущность в радиусе (<see cref="DuelArenaComponent.ScanRange"/>) какой-либо
    /// активной дуэльной арены. Используется, чтобы тегать только то, что заспавнилось на арене
    /// во время боя, а не где угодно на сервере.
    /// </summary>
    private bool IsInActiveDuelRange(EntityUid target)
    {
        var targetXform = Transform(target);
        var pos = targetXform.MapPosition;
        var targetGrid = targetXform.GridUid;
        var query = EntityQueryEnumerator<DuelArenaComponent>();
        while (query.MoveNext(out var arenaUid, out var arena))
        {
            if (!arena.IsActive)
                continue;
            if (InRange(arenaUid, pos, targetGrid, arena.ScanRange))
                return true;
        }
        return false;
    }

    private bool IsDuelActiveNearby(EntityUid originEntity, float range)
    {
        var originXform = Transform(originEntity);
        var origin = originXform.MapPosition;
        var originGrid = originXform.GridUid;
        var query = EntityQueryEnumerator<DuelArenaComponent>();
        while (query.MoveNext(out var arenaUid, out var arena))
        {
            if (!arena.IsActive)
                continue;
            if (InRange(arenaUid, origin, originGrid, range))
                return true;
        }
        return false;
    }

    public void CleanupArea(EntityUid originEntity, float range)
    {
        var originXform = Transform(originEntity);
        var origin = originXform.MapPosition;
        var originGrid = originXform.GridUid;

        // 1. Снаряжение из ящика + гильзы (все помечены ArenaIssuedItemComponent).
        var issuedQuery = EntityQueryEnumerator<ArenaIssuedItemComponent>();
        while (issuedQuery.MoveNext(out var itemUid, out _))
        {
            if (!InRange(itemUid, origin, originGrid, range))
                continue;

            if (Transform(itemUid).Anchored)
                continue;

            // Обувь без статов (нет ускорения/магнитов/анти-скольжения) — косметика, как
            // собственные ботинки игрока. Не удаляем и снимаем тег, чтобы не трогать впредь.
            if (IsStatlessFootwear(itemUid))
            {
                RemCompDeferred<ArenaIssuedItemComponent>(itemUid);
                continue;
            }

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

            // Внутри выданного предмета может сидеть существо (например, соперник в
            // коробке-невидимке StealthBox). Извлекаем мобов перед удалением, иначе QueueDel
            // контейнера каскадно удалит тело соперника вместе с коробкой.
            EjectMobsBeforeDelete(itemUid);

            QueueDel(itemUid);
        }

        // 2. Лужи на полу (кровь, химия и т.п.).
        var puddleQuery = EntityQueryEnumerator<PuddleComponent>();
        while (puddleQuery.MoveNext(out var puddleUid, out _))
        {
            if (!InRange(puddleUid, origin, originGrid, range))
                continue;

            QueueDel(puddleUid);
        }

        // 3. Брёвна, оставшиеся после уничтожения деревьев на арене.
        var logQuery = EntityQueryEnumerator<LogComponent>();
        while (logQuery.MoveNext(out var logUid, out _))
        {
            if (!InRange(logUid, origin, originGrid, range))
                continue;
            if (Transform(logUid).Anchored)
                continue;

            QueueDel(logUid);
        }

        // 4. Сами арена-ящики (Full/Melee Arsenal — помечены markIssuedItems) — убираем вместе
        // с выданным снаряжением, чтобы после боя на арене не оставалось пустых ящиков.
        var crateQuery = EntityQueryEnumerator<SurplusBundleComponent>();
        while (crateQuery.MoveNext(out var crateUid, out var bundle))
        {
            if (!bundle.MarkIssuedItems)
                continue;
            if (!InRange(crateUid, origin, originGrid, range))
                continue;

            QueueDel(crateUid);
        }

        // 5. Останавливаем спавнер дуэльного аирдропа: после конца боя ящики снаряжения
        // больше не падают автоматически (без повторного нажатия кнопки).
        var spawnerQuery = EntityQueryEnumerator<SpawnerSignalControlComponent, TimedSpawnerComponent>();
        while (spawnerQuery.MoveNext(out var spawnerUid, out _, out var timed))
        {
            if (!timed.Enabled)
                continue;
            if (!timed.Prototypes.Contains(DuelSupplyDropProto))
                continue;
            if (!InRange(spawnerUid, origin, originGrid, range))
                continue;

            _spawner.SetEnabled(spawnerUid, timed, false);
        }
    }

    /// <summary>
    /// Обувь без эффектов на характеристики (скорость, магботы, анти-скольжение) — считаем
    /// косметической и не удаляем при очистке арены.
    /// </summary>
    private bool IsStatlessFootwear(EntityUid uid)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing) || (clothing.Slots & SlotFlags.FEET) == 0)
            return false;

        return !HasComp<ClothingSpeedModifierComponent>(uid)
            && !HasComp<MagbootsComponent>(uid)
            && !HasComp<NoSlipComponent>(uid);
    }

    /// <summary>
    /// Цель в зоне арены. Арена — отдельный грид, поэтому при наличии грида у источника охватываем
    /// весь его грид целиком (без ограничения радиусом) — это покрывает всю арену и не задевает
    /// станцию. Если у источника нет грида (в космосе) — откатываемся на проверку по дистанции.
    /// </summary>
    private bool InRange(EntityUid target, MapCoordinates origin, EntityUid? originGrid, float range)
    {
        var targetXform = Transform(target);

        // Весь грид арены — без радиуса.
        if (originGrid != null)
            return targetXform.GridUid == originGrid;

        // Космос/без грида: запасной охват по дистанции.
        var pos = targetXform.MapPosition;
        if (pos.MapId != origin.MapId)
            return false;
        return (pos.Position - origin.Position).Length() <= range;
    }
}
