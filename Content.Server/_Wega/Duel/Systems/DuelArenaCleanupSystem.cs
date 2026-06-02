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
using Content.Shared.Fluids.Components;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
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
public sealed class DuelArenaCleanupSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _link = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ModularSuitSystem _modSuit = default!;
    [Dependency] private readonly SpawnerSystem _spawner = default!;

    /// <summary>Прототип дуэльного ящика-аирдропа — спавнер, кидающий именно его, гасим в конце боя.</summary>
    private static readonly EntProtoId DuelSupplyDropProto = "CrateDuelSupplyDrop";

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

        // 3. Брёвна, оставшиеся после уничтожения деревьев на арене.
        var logQuery = EntityQueryEnumerator<LogComponent>();
        while (logQuery.MoveNext(out var logUid, out _))
        {
            if (!InRange(logUid, origin, range))
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
            if (!InRange(crateUid, origin, range))
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
            if (!InRange(spawnerUid, origin, range))
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

    private bool InRange(EntityUid target, MapCoordinates origin, float range)
    {
        var pos = Transform(target).MapPosition;
        if (pos.MapId != origin.MapId)
            return false;
        return (pos.Position - origin.Position).Length() <= range;
    }
}
