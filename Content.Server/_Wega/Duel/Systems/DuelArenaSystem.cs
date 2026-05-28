using Content.Server._Wega.Duel.Components;
using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Server._Wega.Duel.Systems;

public sealed class DuelArenaSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DuelArenaComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DuelArenaComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnInit(EntityUid uid, DuelArenaComponent comp, ComponentInit args)
    {
        _signalSystem.EnsureSinkPorts(uid, "Open", "Toggle");
    }

    private void OnSignalReceived(EntityUid uid, DuelArenaComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port == "Open")
            StartDuel(uid, comp);
        else if (args.Port == "Toggle")
            ResetDuel(comp);
    }

    private void StartDuel(EntityUid uid, DuelArenaComponent comp)
    {
        comp.Duelists.Clear();
        comp.IsActive = false;

        var trackerPos = Transform(uid).MapPosition;
        var mobQuery = EntityQueryEnumerator<MobStateComponent>();
        while (mobQuery.MoveNext(out var mobUid, out _))
        {
            var mobPos = Transform(mobUid).MapPosition;
            if (mobPos.MapId != trackerPos.MapId)
                continue;
            if ((mobPos.Position - trackerPos.Position).Length() > comp.ScanRange)
                continue;
            if (_mobState.IsAlive(mobUid))
                comp.Duelists.Add(mobUid);
        }

        comp.IsActive = comp.Duelists.Count >= 2;
    }

    private static void ResetDuel(DuelArenaComponent comp)
    {
        comp.Duelists.Clear();
        comp.IsActive = false;
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical && args.NewMobState != MobState.Dead)
            return;

        var uid = args.Target;
        var query = EntityQueryEnumerator<DuelArenaComponent>();
        while (query.MoveNext(out _, out var arena))
        {
            if (!arena.IsActive || !arena.Duelists.Contains(uid))
                continue;

            arena.IsActive = false;

            var loserName = MetaData(uid).EntityName;
            var winner = arena.Duelists.FirstOrDefault(d => d != uid);
            var winnerName = winner != default ? MetaData(winner).EntityName : null;

            var msg = winnerName != null
                ? $"Дуэль завершена! {loserName} потерял сознание. Победитель: {winnerName}!"
                : $"Дуэль завершена! {loserName} потерял сознание.";

            _chatManager.DispatchServerAnnouncement(msg, Color.Gold);
            arena.Duelists.Clear();
            break;
        }
    }
}
