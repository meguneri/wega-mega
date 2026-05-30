using Content.Shared._Wega.Duel.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Duel.Systems;

public sealed class ZonePlayerSensorSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ZonePlayerSensorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ZonePlayerSensorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnInit(EntityUid uid, ZonePlayerSensorComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, comp.OutputPort);
        _deviceLink.EnsureSinkPorts(uid, comp.ResetPort);
    }

    private void OnSignalReceived(EntityUid uid, ZonePlayerSensorComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.ResetPort)
            return;

        comp.LastState = false;
        _deviceLink.SendSignal(uid, comp.OutputPort, false);
    }

    public override void Update(float deltaTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ZonePlayerSensorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextCheck > now)
                continue;

            comp.NextCheck = now + comp.CheckDelay;
            UpdateOutput(uid, comp);
        }
    }

    private void UpdateOutput(EntityUid uid, ZonePlayerSensorComponent comp)
    {
        if (comp.LastState)
            return;

        var sensorPos = Transform(uid).MapPosition;

        var mobQuery = EntityQueryEnumerator<MobStateComponent>();
        while (mobQuery.MoveNext(out var mobUid, out _))
        {
            var mobPos = Transform(mobUid).MapPosition;
            if (mobPos.MapId != sensorPos.MapId)
                continue;
            if ((mobPos.Position - sensorPos.Position).Length() > comp.Range)
                continue;
            if (!_mobState.IsAlive(mobUid))
                continue;

            comp.LastState = true;
            _deviceLink.SendSignal(uid, comp.OutputPort, true);
            return;
        }
    }
}
