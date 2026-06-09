using Content.Server._Wega.DeviceLinking.Components;
using Content.Server.Audio;
using Content.Shared.DeviceLinking.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Wega.DeviceLinking.Systems;

/// <summary>
/// Plays a station-wide sound when an entity with
/// <see cref="EmitGlobalSoundOnSignalComponent"/> receives any device-link signal.
/// </summary>
public sealed partial class EmitGlobalSoundOnSignalSystem : EntitySystem
{
    [Dependency] private ServerGlobalSoundSystem _globalSound = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmitGlobalSoundOnSignalComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnSignalReceived(EntityUid uid, EmitGlobalSoundOnSignalComponent component, ref SignalReceivedEvent args)
    {
        _globalSound.PlayGlobalOnStation(uid, _audio.ResolveSound(component.Sound), component.Params);
    }
}
