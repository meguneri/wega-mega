using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.Selector.UI;
using Content.Shared.Item.Selector.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Server._Wega.Duel.Components;
using Content.Server._Wega.Duel.Systems;

namespace Content.Server.Item.Selector;

public sealed partial class ItemSelectorSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _admin = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private DuelArenaCleanupSystem _arenaCleanup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ItemSelectorComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ItemSelectorComponent, ItemSelectorSelectionMessage>(OnSelection);
    }

    private void OnUiOpened(EntityUid uid, ItemSelectorComponent comp, BoundUIOpenedEvent args)
    {
        if (!CheckComponents(args.Actor, comp.WhitelistComponents, comp.BlacklistComponents))
        {
            _ui.CloseUi(uid, ItemSelectorUiKey.Key);
            return;
        }

        UpdateUi(uid, comp.Items);
    }

    private bool CheckComponents(EntityUid entity, List<string> whitelist, List<string> blacklist)
    {
        if (whitelist.Count > 0 && !whitelist.All(component =>
            _componentFactory.TryGetRegistration(component, out var reg) && HasComp(entity, reg.Type)))
            return false;

        if (blacklist.Count > 0 && blacklist.Any(component =>
            _componentFactory.TryGetRegistration(component, out var reg) && HasComp(entity, reg.Type)))
            return false;

        return true;
    }

    private void UpdateUi(EntityUid uid, List<EntProtoId> items)
    {
        if (!_ui.HasUi(uid, ItemSelectorUiKey.Key))
            return;

        _ui.ServerSendUiMessage(uid, ItemSelectorUiKey.Key,
            new ItemSelectorUserMessage(items));
    }

    private void OnSelection(EntityUid uid, ItemSelectorComponent comp, ItemSelectorSelectionMessage args)
    {
        var ent = Spawn(args.SelectedId, Transform(uid).Coordinates);

        // Если сам селектор был выдан дуэльной ареной (помечен ArenaIssuedItemComponent),
        // переносим тег на заспавненный предмет (и его содержимое), иначе очистка арены не
        // уберёт выбранный скафандр/гипоручку — они спавнятся свежими и без тега.
        if (HasComp<ArenaIssuedItemComponent>(uid))
            _arenaCleanup.MarkIssuedRecursive(ent);

        _hands.TryForcePickupAnyHand(GetEntity(args.User), ent);

        _admin.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(GetEntity(args.User)):user} selects a {ToPrettyString(ent):entity} instead of {ToPrettyString(uid):entity}");

        QueueDel(uid);
    }
}
