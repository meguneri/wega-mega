using System.Linq;
using System.Numerics;
using Content.Server._Wega.Duel.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Ready-check кнопки дуэли. Первое нажатие помечает бойца готовым и вешает над ним голограмму
/// «ГОТОВ»; повторное — снимает готовность. Когда готовы все живые игроки на арене (минимум 2),
/// кнопка программно шлёт DuelStart и запускается штатная цепочка старта (таймер → DuelFight →
/// барьеры/колокол/ArmDuel). Готовность хранится на трекере (<see cref="DuelArenaComponent"/>),
/// кнопка находит «свою» арену по гриду.
/// </summary>
public sealed partial class DuelArenaSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    private void InitializeReady()
    {
        SubscribeLocalEvent<DuelReadyButtonComponent, ComponentInit>(OnReadyButtonInit);
        SubscribeLocalEvent<DuelReadyButtonComponent, ActivateInWorldEvent>(OnReadyButtonActivated);
    }

    // Гарантируем регистрацию порта старта до того, как AutoLink (на MapInit) свяжет кнопку с
    // таймером — иначе программный InvokePort(StartPort) не дошёл бы до таймера. YAML ports: [Pressed]
    // обычно уже достаточно; это страховка в духе EnsureSourcePorts на трекере.
    private void OnReadyButtonInit(EntityUid uid, DuelReadyButtonComponent comp, ComponentInit args)
    {
        _signalSystem.EnsureSourcePorts(uid, comp.StartPort);
    }

    private void OnReadyButtonActivated(EntityUid uid, DuelReadyButtonComponent comp, ActivateInWorldEvent args)
    {
        // args.Complex — осознанное «использование» (E/клик), а не побочное взаимодействие.
        if (args.Handled || !args.Complex)
            return;

        if (!TryGetArenaForGrid(Transform(uid).GridUid, out var arenaUid, out var arena))
            return;

        args.Handled = true;

        // Во время боя кнопка не работает — готовность собирается только до старта.
        if (arena.IsActive)
        {
            _popup.PopupEntity(Loc.GetString("duel-ready-already-active"), args.User, args.User);
            return;
        }

        var user = args.User;

        // Тоггл готовности нажавшего.
        if (arena.Ready.Contains(user))
            SetUnready(arena, user);
        else
            SetReady(arena, user);

        // Кто вообще должен подтвердить готовность (живые игроки на гриде арены), и чистка тех,
        // кто уже ушёл с арены, но остался в списке готовых.
        var required = GetReadyRequired(arenaUid, arena);
        PruneReady(arena, required);

        var isReady = arena.Ready.Contains(user);
        var msg = Loc.GetString(
            isReady ? "duel-ready-fighter-ready" : "duel-ready-fighter-unready",
            ("name", SafeName(user)), ("count", arena.Ready.Count), ("total", required.Count));

        // Индикатор-объявление видно всем рядом; основной индикатор — голограмма над бойцом.
        _popup.PopupCoordinates(msg, Transform(uid).Coordinates, PopupType.Medium);
        if (arena.ReadySound != null)
            _audio.PlayPvs(arena.ReadySound, uid);

        // Старт, когда готовы все нужные бойцы (минимум двое).
        if (required.Count >= 2 && required.All(arena.Ready.Contains))
        {
            ClearReady(arena);
            // Программно дёргаем порт кнопки — AutoLink доставит DuelStart таймеру, дальше штатно.
            _signalSystem.InvokePort(uid, comp.StartPort);
        }
    }

    private void SetReady(DuelArenaComponent arena, EntityUid user)
    {
        arena.Ready.Add(user);

        // Голограмма «ГОТОВ» висит над бойцом (привязана к нему — следует за ним).
        if (!arena.ReadyHolograms.ContainsKey(user) && Exists(user))
            arena.ReadyHolograms[user] = Spawn(arena.ReadyHologram, new EntityCoordinates(user, new Vector2(0f, 0.75f)));
    }

    private void SetUnready(DuelArenaComponent arena, EntityUid user)
    {
        arena.Ready.Remove(user);
        if (arena.ReadyHolograms.Remove(user, out var holo) && Exists(holo))
            QueueDel(holo);
    }

    /// <summary>Живые игроки (управляемые сессией) на гриде арены — те, кто должен подтвердить готовность.
    /// Совпадает с набором бойцов, которых ArmDuel зарегистрирует при старте.</summary>
    private List<EntityUid> GetReadyRequired(EntityUid arenaUid, DuelArenaComponent arena)
        => GetAliveInRange(arenaUid, arena).Where(d => GetUser(d) != null).ToList();

    /// <summary>Снимает готовность с тех, кого уже нет среди требуемых бойцов (ушёл с арены/погиб).</summary>
    private void PruneReady(DuelArenaComponent arena, List<EntityUid> required)
    {
        foreach (var u in arena.Ready.ToList())
        {
            if (!required.Contains(u))
                SetUnready(arena, u);
        }
    }

    /// <summary>Сбрасывает готовность и убирает все голограммы. Вызывается при старте/завершении/сбросе боя.</summary>
    public void ClearReady(DuelArenaComponent arena)
    {
        foreach (var holo in arena.ReadyHolograms.Values)
        {
            if (Exists(holo))
                QueueDel(holo);
        }
        arena.ReadyHolograms.Clear();
        arena.Ready.Clear();
    }

    private bool TryGetArenaForGrid(EntityUid? grid, out EntityUid arenaUid, out DuelArenaComponent arena)
    {
        arenaUid = default;
        arena = default!;
        if (grid == null)
            return false;

        var query = EntityQueryEnumerator<DuelArenaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (Transform(uid).GridUid != grid)
                continue;
            arenaUid = uid;
            arena = comp;
            return true;
        }
        return false;
    }
}
