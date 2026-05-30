using Content.Server._Wega.Duel.Components;
using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Трекер дуэльной арены. Автоматически следит за зоной вокруг себя:
/// как только в радиусе оказывается ровно двое живых — дуэль «вооружается».
/// Когда один из дуэлянтов теряет сознание (крит/смерть) — объявляет победителя
/// на весь сервер и шлёт сигнал закрытия барьеров.
/// </summary>
public sealed class DuelArenaSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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
        _signalSystem.EnsureSourcePorts(uid, comp.ResetPort);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<DuelArenaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            // Сканируем только вооружённые арены — чтобы вовремя снять взвод,
            // если дуэлянты разошлись живыми и победа в OnMobStateChanged не наступит.
            if (!comp.IsActive || now < comp.NextScan)
                continue;

            comp.NextScan = now + TimeSpan.FromSeconds(comp.ScanInterval);
            Scan(uid, comp);
        }
    }

    /// <summary>
    /// Снимает дуэль с боевого взвода, если в зоне не осталось ни одного из запомненных дуэлянтов
    /// (например, оба покинули арену живыми, и финала по крит/смерти не будет).
    /// </summary>
    private void Scan(EntityUid uid, DuelArenaComponent comp)
    {
        var alive = GetAliveInRange(uid, comp);
        if (!comp.Duelists.Any(d => alive.Contains(d)))
            ResetDuel(comp);
    }

    /// <summary>
    /// Собирает живых мобов в радиусе арены.
    /// </summary>
    private HashSet<EntityUid> GetAliveInRange(EntityUid uid, DuelArenaComponent comp)
    {
        var trackerPos = Transform(uid).MapPosition;

        var alive = new HashSet<EntityUid>();
        var mobQuery = EntityQueryEnumerator<MobStateComponent>();
        while (mobQuery.MoveNext(out var mobUid, out _))
        {
            var mobPos = Transform(mobUid).MapPosition;
            if (mobPos.MapId != trackerPos.MapId)
                continue;
            if ((mobPos.Position - trackerPos.Position).Length() > comp.ScanRange)
                continue;
            if (_mobState.IsAlive(mobUid))
                alive.Add(mobUid);
        }

        return alive;
    }

    /// <summary>
    /// Вооружает дуэль: запоминает живых бойцов в арене на момент старта.
    /// Вызывается по сигналу кнопки старта (порт Open), а не по подсчёту присутствующих.
    /// </summary>
    private void ArmDuel(EntityUid uid, DuelArenaComponent comp)
    {
        if (comp.IsActive)
            return;

        var duelists = GetAliveInRange(uid, comp);
        if (duelists.Count == 0)
            return;

        comp.Duelists.Clear();
        foreach (var d in duelists)
            comp.Duelists.Add(d);
        comp.IsActive = true;

        var names = string.Join(" против ", comp.Duelists.Select(d => MetaData(d).EntityName));
        _chatManager.DispatchServerAnnouncement($"Дуэль началась! {names}", Color.Gold);
    }

    private void OnSignalReceived(EntityUid uid, DuelArenaComponent comp, ref SignalReceivedEvent args)
    {
        switch (args.Port)
        {
            // Сигнал старта (после отсчёта таймера) — вооружаем дуэль.
            case "Open":
                ArmDuel(uid, comp);
                break;
            // Ручной сброс с кнопки сброса.
            case "Toggle":
                ResetDuel(comp);
                break;
        }
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
        while (query.MoveNext(out var arenaUid, out var arena))
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
            _signalSystem.SendSignal(arenaUid, arena.ResetPort, true);
            break;
        }
    }
}
