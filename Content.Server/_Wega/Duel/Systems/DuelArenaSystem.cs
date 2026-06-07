using Content.Server._Wega.Duel.Components;
using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
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

            // Авто-дроп снабжения во время активного боя: сбрасываем маяк в центр арены
            // (он сам даёт колокол/свет и спавнит ящик), затем перепланируем по интервалу.
            if (comp.IsActive && comp.SupplyDropProto != null
                && comp.SupplyDropAt != null && now >= comp.SupplyDropAt)
            {
                Spawn(comp.SupplyDropProto.Value, Transform(uid).Coordinates);
                comp.SupplyDropAt = comp.SupplyDropInterval > 0f
                    ? now + TimeSpan.FromSeconds(comp.SupplyDropInterval)
                    : null;
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
    /// Периодическая проверка исхода боя. Подстраховывает событие крит/смерти: если его не
    /// поймали вовремя и в живых остался ≤1 дуэлянт — объявляем итог здесь. Если оба ещё живы,
    /// но никого из них нет в зоне — дуэль заброшена, тихо снимаем взвод (без победителя).
    /// </summary>
    private void Scan(EntityUid uid, DuelArenaComponent comp)
    {
        var aliveDuelists = comp.Duelists.Where(IsAliveDuelist).ToList();
        if (aliveDuelists.Count <= 1)
        {
            ConcludeDuel(uid, comp);
            return;
        }

        var aliveInRange = GetAliveInRange(uid, comp);
        if (!comp.Duelists.Any(d => aliveInRange.Contains(d)))
            ResetDuel(comp);
    }

    /// <summary>
    /// «Ещё в бою» ли дуэлянт. Боец считается стоящим, пока он не в крите и не мёртв —
    /// предкрит (PreCritical) НЕ завершает дуэль. Безопасно к удалённым сущностям.
    /// </summary>
    private bool IsAliveDuelist(EntityUid d)
        => Exists(d) && HasComp<MobStateComponent>(d) && !_mobState.IsIncapacitated(d);

    private string SafeName(EntityUid uid)
        => Exists(uid) ? MetaData(uid).EntityName : "?";

    /// <summary>
    /// Собирает живых дуэлянтов-гуманоидов на арене. Учитываются только гуманоиды (игроки):
    /// мыши, обезьяны и прочие мобы не должны регистрироваться бойцами и блокировать сброс
    /// заброшенной дуэли. Арена — отдельный грид, поэтому ограничиваем охват гридом трекера:
    /// это покрывает всю арену и гарантированно не цепляет станцию. Дистанция (ScanRange)
    /// остаётся дополнительной страховкой.
    /// </summary>
    private HashSet<EntityUid> GetAliveInRange(EntityUid uid, DuelArenaComponent comp)
    {
        var trackerXform = Transform(uid);
        var trackerPos = trackerXform.MapPosition;
        var trackerGrid = trackerXform.GridUid;

        var alive = new HashSet<EntityUid>();
        var mobQuery = EntityQueryEnumerator<MobStateComponent, HumanoidProfileComponent>();
        while (mobQuery.MoveNext(out var mobUid, out _, out _))
        {
            var mobXform = Transform(mobUid);
            if (trackerGrid != null && mobXform.GridUid != trackerGrid)
                continue;

            var mobPos = mobXform.MapPosition;
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
        // Сигнал старта может прийти повторно (двойное нажатие/повтор сигнала). Если дуэль уже
        // идёт — молча игнорируем, чтобы не дублировать объявление «Дуэль началась».
        if (comp.IsActive)
            return;

        var duelists = GetAliveInRange(uid, comp);
        if (duelists.Count < 2)
        {
            _chatManager.DispatchServerAnnouncement(
                Loc.GetString(duelists.Count == 0
                    ? "duel-arena-not-started-no-fighters"
                    : "duel-arena-not-started-need-two"),
                Color.Gray);
            return;
        }

        comp.Duelists.Clear();
        foreach (var d in duelists)
            comp.Duelists.Add(d);
        comp.IsActive = true;

        // Отменяем grace-период предыдущей дуэли — иначе Update отправит сигнал закрытия
        // шлюзов уже во время нового боя.
        comp.GateCloseAt = null;
        _signalSystem.SendSignal(uid, comp.ResetPort, false);

        // Планируем первый авто-дроп снабжения (если включён для этой арены).
        comp.SupplyDropAt = comp.SupplyDropProto != null
            ? _timing.CurTime + TimeSpan.FromSeconds(comp.SupplyDropDelay)
            : null;

        var vsSep = $" {Loc.GetString("duel-arena-connector-vs")} ";
        var names = string.Join(vsSep, comp.Duelists.Select(d => MetaData(d).EntityName));
        _chatManager.DispatchServerAnnouncement(
            Loc.GetString("duel-arena-started", ("fighters", names)), Color.Gold);
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

        // Останавливаем авто-дроп снабжения до следующего боя.
        comp.SupplyDropAt = null;

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
            comp.ScoreNames.Clear();
            comp.StreakUser = null;
            comp.Streak = 0;
            cleared++;
        }

        if (cleared > 0)
            _chatManager.DispatchServerAnnouncement(Loc.GetString("duel-arena-scores-reset"), Color.Gold);

        return cleared;
    }

    /// <summary>
    /// Возвращает идентификатор игрока, управляющего телом, или null для тел без разума (NPC).
    /// </summary>
    private NetUserId? GetUser(EntityUid body)
    {
        return _mind.TryGetMind(body, out _, out var mind) ? mind.UserId : null;
    }

    /// <summary>
    /// Собирает строку общего счёта арены: «Имя — N», сортировка по убыванию побед, затем по имени.
    /// Возвращает null, если счёта ещё нет.
    /// </summary>
    private string? BuildScoreboard(DuelArenaComponent arena)
    {
        if (arena.Scores.Count == 0)
            return null;

        var entries = arena.Scores
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => arena.ScoreNames.GetValueOrDefault(kv.Key, "?"))
            .Select(kv => $"{arena.ScoreNames.GetValueOrDefault(kv.Key, "?")} — {kv.Value}");

        return string.Join(", ", entries);
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

            ConcludeDuel(arenaUid, arena);
            break;
        }
    }

    /// <summary>
    /// Подводит итог дуэли, если в живых остался ≤1 дуэлянт: объявляет победителя (или ничью),
    /// начисляет счёт, убирает выданное снаряжение и запускает grace-период закрытия шлюзов.
    /// Если живых ещё ≥2 — ничего не делает (бой продолжается). Идемпотентна за счёт IsActive.
    /// </summary>
    private bool ConcludeDuel(EntityUid arenaUid, DuelArenaComponent arena)
    {
        if (!arena.IsActive)
            return false;

        var aliveDuelists = arena.Duelists.Where(IsAliveDuelist).ToList();
        if (aliveDuelists.Count > 1)
            return false; // бой ещё идёт

        arena.IsActive = false;

        // Останавливаем авто-дроп снабжения — бой окончен.
        arena.SupplyDropAt = null;

        EntityUid? winner = aliveDuelists.Count == 1 ? aliveDuelists[0] : null;

        // Запоминаем актуальные имена всех бойцов этого боя по их NetUserId — чтобы общий счёт
        // ниже отображался с именами, даже если кто-то из них не участвует в следующих раундах.
        foreach (var duelist in arena.Duelists)
        {
            var user = GetUser(duelist);
            if (user != null)
                arena.ScoreNames[user.Value] = SafeName(duelist);
        }

        string msg;
        if (winner != null)
        {
            var winnerName = SafeName(winner.Value);

            // Проигравшие — все остальные зарегистрированные бойцы (для дуэлей 3+ их несколько).
            var losers = arena.Duelists.Where(d => d != winner.Value).ToList();
            var loserNames = losers.Count > 0
                ? string.Join(", ", losers.Select(SafeName))
                : Loc.GetString("duel-arena-losers-fallback");

            // Счёт ведём по игроку (NetUserId), а не по телу: иначе после клона/респавна
            // боец получает новый EntityUid и счёт каждый раунд начинается заново.
            var winnerUser = GetUser(winner.Value);

            if (winnerUser != null)
                arena.Scores[winnerUser.Value] = arena.Scores.GetValueOrDefault(winnerUser.Value) + 1;

            // Серия побед подряд: растёт, если победил тот же игрок, иначе начинается заново.
            if (winnerUser != null && arena.StreakUser == winnerUser)
                arena.Streak++;
            else
            {
                arena.StreakUser = winnerUser;
                arena.Streak = 1;
            }

            msg = Loc.GetString("duel-arena-concluded-winner",
                ("winner", winnerName),
                ("streak", arena.Streak),
                ("losers", loserNames),
                ("loserCount", losers.Count));
        }
        else
        {
            // Никого живого — ничья: серия прерывается.
            arena.StreakUser = null;
            arena.Streak = 0;

            var andSep = $" {Loc.GetString("duel-arena-connector-and")} ";
            var names = string.Join(andSep, arena.Duelists.Select(SafeName));
            msg = Loc.GetString("duel-arena-concluded-draw", ("fighters", names));
        }

        arena.Duelists.Clear();

        // Дописываем общий накопленный счёт арены (все игроки, сортировка по убыванию побед).
        var scoreboard = BuildScoreboard(arena);
        if (scoreboard != null)
            msg += "\n" + Loc.GetString("duel-arena-scoreboard", ("scores", scoreboard));

        // Убираем снаряжение и объявляем результат одним сообщением.
        _cleanup.CleanupArea(arenaUid, arena.CleanupRange);
        _chatManager.DispatchServerAnnouncement(msg, Color.Gold);

        // Сигнал закрытия шлюзов шлём не сразу, а через ReturnGrace секунд: дуэлянты
        // возвращаются в базы по открытым шлюзам, и только потом те закрываются (см. Update).
        arena.GateCloseAt = _timing.CurTime + TimeSpan.FromSeconds(arena.ReturnGrace);
        return true;
    }
}
