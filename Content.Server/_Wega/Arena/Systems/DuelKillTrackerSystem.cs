using System.Linq;
using Content.Server._Wega.Arena.Components;
using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;

namespace Content.Server._Wega.Arena.Systems;

/// <summary>
/// Listens for deaths near a <see cref="DuelKillTrackerComponent"/> and
/// announces the running kill score to all players.
///
/// Signal wiring (AutoLink, no manual editor setup needed):
///   DuelFight  → activates the tracker (start counting)
///   DuelReset  → resets scores and deactivates
/// </summary>
public sealed class DuelKillTrackerSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
[Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DuelKillTrackerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DuelKillTrackerComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnInit(EntityUid uid, DuelKillTrackerComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, "Open", "Toggle");
    }

    private void OnSignalReceived(EntityUid uid, DuelKillTrackerComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port == comp.StartPort)
        {
            comp.Active = true;
            _chat.DispatchGlobalAnnouncement(
                "Дуэль началась!",
                sender: comp.Sender,
                playSound: false,
                colorOverride: Color.Yellow);
        }
        else if (args.Port == comp.ResetPort)
        {
            if (comp.Kills.Count > 0)
                AnnounceScore(uid, comp);

            comp.Active = false;

            _chat.DispatchGlobalAnnouncement(
                "Дуэль завершена. Счётчик сброшен.",
                sender: comp.Sender,
                playSound: false,
                colorOverride: Color.Gray);
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        // Find the name of the player who died.
        var victimName = Name(args.Target);

        var trackerQuery = EntityQueryEnumerator<DuelKillTrackerComponent, TransformComponent>();
        while (trackerQuery.MoveNext(out var trackerUid, out var tracker, out var trackerXform))
        {
            if (!tracker.Active)
                continue;

            var victimXform = Transform(args.Target);
            if (!_transform.InRange(trackerXform.Coordinates, victimXform.Coordinates, tracker.Range))
                continue;

            // Find the killer — whoever caused the final blow.
            var killerName = GetKillerName(args.Origin);

            if (killerName == null || killerName == victimName)
            {
                // Suicide / environmental — penalise the victim.
                tracker.Kills.TryGetValue(victimName, out var current);
                // Don't add a kill, just announce.
                _chat.DispatchGlobalAnnouncement(
                    $"{victimName} погиб от собственной глупости.",
                    sender: tracker.Sender,
                    playSound: false,
                    colorOverride: Color.Orange);
            }
            else
            {
                tracker.Kills.TryGetValue(killerName, out var kills);
                tracker.Kills[killerName] = kills + 1;

                _chat.DispatchGlobalAnnouncement(
                    $"{killerName} убил {victimName}! Счёт: {tracker.Kills[killerName]}",
                    sender: tracker.Sender,
                    playSound: false,
                    colorOverride: Color.LimeGreen);
            }

            AnnounceScore(trackerUid, tracker);
        }
    }

    private void AnnounceScore(EntityUid uid, DuelKillTrackerComponent comp)
    {
        if (comp.Kills.Count == 0)
            return;

        var sorted = comp.Kills
            .OrderByDescending(kv => kv.Value)
            .Select(kv => $"{kv.Key}: {kv.Value}")
            .ToList();

        var scoreText = string.Join("  |  ", sorted);
        _chat.DispatchGlobalAnnouncement(
            $"Счёт: {scoreText}",
            sender: comp.Sender,
            playSound: false,
            colorOverride: Color.Cyan);
    }

    private string? GetKillerName(EntityUid? origin)
    {
        if (origin == null)
            return null;
        return Name(origin.Value);
    }
}
