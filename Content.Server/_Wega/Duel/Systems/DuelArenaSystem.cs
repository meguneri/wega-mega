using Content.Server._Wega.Duel.Components;
using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Трекер дуэльной арены. По сигналу старта запоминает бойцов в зоне; когда один теряет
/// сознание (крит/смерть) — объявляет победителя на весь сервер. Сигнал закрытия шлюзов
/// (порт <see cref="DuelArenaComponent.ResetPort"/>) отправляется не сразу, а спустя
/// <see cref="DuelArenaComponent.ReturnGrace"/> секунд — чтобы дуэлянты успели вернуться в базы.
/// </summary>
public sealed class DuelArenaSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly DuelArenaCleanupSystem _cleanup = default!;
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
            // Истёк grace-период после боя — шлём на шлюзы баз сигнал закрытия.
            // Дуэлянты уже успели вернуться по открытым шлюзам.
            if (comp.GateCloseAt != null && now >= comp.GateCloseAt)
            {
                comp.GateCloseAt = null;
                _signalSystem.SendSignal(uid, comp.ResetPort, true);
            }

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

        // Бой начался: отменяем запланированное закрытие шлюзов и сбрасываем сигнал в LOW.
        // Шлюзы открываются по сигналу старта (DuelFight), а сброс латча гарантирует, что
        // следующее закрытие будет «свежим» фронтом.
        comp.GateCloseAt = null;
        _signalSystem.SendSignal(uid, comp.ResetPort, false);

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
            // Ручной сброс текущего боя (кнопка сброса). Накопленный счёт не трогает —
            // его обнуляет только админ-команда duelscorereset.
            case "Toggle":
                ResetDuel(comp);
                break;
        }
    }

    private void ResetDuel(DuelArenaComponent comp)
    {
        comp.Duelists.Clear();
        comp.IsActive = false;

        // Шлюзы закроем через ReturnGrace секунд — чтобы бойцы успели вернуться в свои базы.
        comp.GateCloseAt = _timing.CurTime + TimeSpan.FromSeconds(comp.ReturnGrace);
    }

    /// <summary>
    /// Обнуляет накопленный счёт на всех дуэльных аренах. Вызывается админ-командой duelscorereset.
    /// Возвращает число арен, на которых счёт был непустым.
    /// </summary>
    public int ResetAllScores()
    {
        var cleared = 0;
        var query = EntityQueryEnumerator<DuelArenaComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            if (comp.Scores.Count == 0)
                continue;

            comp.Scores.Clear();
            cleared++;
        }

        if (cleared > 0)
            _chatManager.DispatchServerAnnouncement("Счёт дуэльной арены обнулён.", Color.Gold);

        return cleared;
    }

    /// <summary>
    /// Возвращает идентификатор игрока, управляющего телом, или null для тел без разума (NPC).
    /// </summary>
    private NetUserId? GetUser(EntityUid body)
    {
        return _mind.TryGetMind(body, out _, out var mind) ? mind.UserId : null;
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

            var loser = uid;
            var loserName = MetaData(loser).EntityName;
            var winner = arena.Duelists.FirstOrDefault(d => d != loser);

            string msg;
            if (winner != default)
            {
                var winnerName = MetaData(winner).EntityName;

                // Счёт ведём по игроку (NetUserId), а не по телу: иначе после клона/респавна
                // боец получает новый EntityUid и счёт каждый раунд начинается заново.
                var winnerUser = GetUser(winner);
                var loserUser = GetUser(loser);

                if (winnerUser != null)
                    arena.Scores[winnerUser.Value] = arena.Scores.GetValueOrDefault(winnerUser.Value) + 1;

                var winnerScore = winnerUser != null ? arena.Scores.GetValueOrDefault(winnerUser.Value) : 0;
                var loserScore = loserUser != null ? arena.Scores.GetValueOrDefault(loserUser.Value) : 0;

                msg = $"Дуэль завершена! {loserName} потерял сознание. Победитель: {winnerName}! " +
                      $"Счёт: {winnerName} {winnerScore} — {loserScore} {loserName}";
            }
            else
            {
                msg = $"Дуэль завершена! {loserName} потерял сознание.";
            }

            _chatManager.DispatchServerAnnouncement(msg, Color.Gold);
            arena.Duelists.Clear();

            // По концу боя сразу убираем снаряжение с бойцов и предметы с пола арены
            // (базовую одежду — комбинезон/бельё/чулки — не трогаем).
            _cleanup.CleanupArea(arenaUid, arena.ScanRange);
            _chatManager.DispatchServerAnnouncement("Арена очищена: снаряжение дуэлянтов и предметы убраны.", Color.Gold);

            // Сигнал закрытия шлюзов шлём не сразу, а через ReturnGrace секунд: дуэлянты
            // возвращаются в базы по открытым шлюзам, и только потом те закрываются (см. Update).
            arena.GateCloseAt = _timing.CurTime + TimeSpan.FromSeconds(arena.ReturnGrace);
            break;
        }
    }
}
