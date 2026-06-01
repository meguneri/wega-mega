using Content.Server._Wega.Duel.Components;
using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Fluids.Components;
using Content.Shared.Implants.Components;
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

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DuelArenaCleanupComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DuelArenaCleanupComponent, SignalReceivedEvent>(OnSignalReceived);

        // Тегаем гильзы и прочие картриджи, заспавненные во время активной дуэли,
        // чтобы клинап их убрал независимо от того, из чьего оружия они вылетели.
        SubscribeLocalEvent<CartridgeAmmoComponent, ComponentStartup>(OnCartridgeStartup);
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
