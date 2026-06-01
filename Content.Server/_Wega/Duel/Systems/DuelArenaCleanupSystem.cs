using Content.Server._Wega.Duel.Components;
using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Fluids.Components;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Очистка дуэльной арены: удаляет только предметы, выданные ящиком-арсеналом
/// (помеченные <see cref="ArenaIssuedItemComponent"/>), где бы они ни лежали — на полу,
/// в руках, надетые на бойцах, внутри рюкзаков/ящиков или вколотые в дуэлянтов импланты.
/// Вещи игроков и предметы карты НЕ трогаются. Дополнительно убираются лужи (кровь, химия
/// и т.п.) — следы боя.
///
/// Вызывается автоматически по концу боя (<see cref="DuelArenaSystem"/>) и вручную кнопкой
/// через <see cref="DuelArenaCleanupComponent"/>.
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
    }

    private void OnInit(EntityUid uid, DuelArenaCleanupComponent comp, ComponentInit args)
    {
        _link.EnsureSinkPorts(uid, comp.TriggerPort);
    }

    private void OnSignalReceived(EntityUid uid, DuelArenaCleanupComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.TriggerPort)
            return;

        // Защита от «очистки на старте»: если рядом есть арена с идущим боём (IsActive),
        // игнорируем сигнал. Спасает от случайной проводки старт-устройств на порт Trigger,
        // а заодно не даёт ручной кнопке зачистить арену посреди дуэли. В конце боя очистка
        // идёт напрямую из DuelArenaSystem (там IsActive уже сброшен), так что её не блокирует.
        if (IsDuelActiveNearby(uid, comp.Range))
            return;

        CleanupArea(uid, comp.Range);
        _chat.DispatchServerAnnouncement("Арена очищена: выданное снаряжение убрано.", Color.Gold);
    }

    /// <summary>
    /// Есть ли в радиусе от контроллера дуэльная арена с идущим боём.
    /// </summary>
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

    /// <summary>
    /// Удаляет в радиусе только предметы, выданные ящиком-арсеналом (помеченные
    /// <see cref="ArenaIssuedItemComponent"/>), и лужи. Удаление предмета-контейнера
    /// (рюкзака/ящика) забирает с собой и его содержимое. Вколотые дуэлянтами импланты
    /// сперва принудительно извлекаются, чтобы снять дарованные ими действия/компоненты.
    /// Центр и радиус задаёт вызывающая сторона.
    /// </summary>
    public void CleanupArea(EntityUid originEntity, float range)
    {
        var origin = Transform(originEntity).MapPosition;

        // 1. Только выданное снаряжение — на полу, в руках, надетое, в контейнерах или вколотое.
        var issuedQuery = EntityQueryEnumerator<ArenaIssuedItemComponent>();
        while (issuedQuery.MoveNext(out var itemUid, out _))
        {
            if (!InRange(itemUid, origin, range))
                continue;

            // Вколотый имплант: ПРИНУДИТЕЛЬНО вынимаем из импланта-контейнера дуэлянта (force обходит
            // запрет на извлечение перманентных). Это поднимает EntGotRemovedFromContainerMessage, и
            // SharedSubdermalImplantSystem снимает дарованные действия/компоненты. Без этого простой
            // QueueDel удалил бы только сущность импланта, а действие (напр. «побег») осталось бы рабочим.
            if (_container.TryGetContainingContainer((itemUid, null), out var container)
                && container.ID == ImplanterComponent.ImplantSlotId)
            {
                _container.Remove(itemUid, container, reparent: false, force: true);
            }

            QueueDel(itemUid);
        }

        // 2. Лужи на полу (кровь, химия и т.п.) — следы боя.
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
