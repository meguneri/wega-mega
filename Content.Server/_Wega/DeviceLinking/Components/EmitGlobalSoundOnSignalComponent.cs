using Robust.Shared.Audio;

namespace Content.Server._Wega.DeviceLinking.Components;

/// <summary>
/// Plays a station-wide sound when this entity receives a device-link signal.
/// </summary>
[RegisterComponent]
public sealed partial class EmitGlobalSoundOnSignalComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public AudioParams Params = AudioParams.Default;
}
