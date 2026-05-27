using Content.Server.Chat.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Spawners.Components;
using Content.Server.Spawners.EntitySystems;
using Content.Shared._Wega.Spawners.Components;
using Content.Shared.DeviceLinking.Events;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server._Wega.Spawners.EntitySystems;

/// <summary>
/// Handles device-link signals for <see cref="SpawnerSignalControlComponent"/>.
/// A Toggle signal flips <see cref="TimedSpawnerComponent.Enabled"/>.
/// When the spawner transitions from disabled → enabled the timer is reset so the
/// first spawn happens after the full interval (not immediately).
/// Broadcasts a global chat announcement on every toggle so all players know the state.
/// </summary>
[UsedImplicitly]
public sealed class SpawnerSignalControlSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SpawnerSystem _spawner = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnerSignalControlComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpawnerSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, SpawnerSignalControlComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.TogglePort);
    }

    private void OnSignalReceived(EntityUid uid, SpawnerSignalControlComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.TogglePort)
            return;

        if (!TryComp<TimedSpawnerComponent>(uid, out var timedSpawner))
            return;

        // Remember the source entity so spawns happen near it (e.g. the button on the wall).
        if (args.Trigger is { } triggerUid && Exists(triggerUid))
            timedSpawner.SpawnNearEntity = triggerUid;

        // Toggle: disable if enabled, enable if disabled.
        _spawner.SetEnabled(uid, timedSpawner, !timedSpawner.Enabled);

        // Announce the new state to all players.
        if (timedSpawner.Enabled)
        {
            _chat.DispatchGlobalAnnouncement(
                comp.EnabledMessage,
                sender: comp.AnnounceSender,
                playSound: false,
                colorOverride: Color.Orange);
        }
        else
        {
            _chat.DispatchGlobalAnnouncement(
                comp.DisabledMessage,
                sender: comp.AnnounceSender,
                playSound: false,
                colorOverride: Color.Gray);
        }
    }
}
