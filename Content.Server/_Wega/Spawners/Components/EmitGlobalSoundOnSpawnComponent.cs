using Robust.Shared.Audio;

namespace Content.Server._Wega.Spawners.Components;

/// <summary>
/// Plays a sound globally across the entire station when this entity spawns.
/// Unlike EmitSoundOnSpawnComponent, this uses PlayGlobalOnStation
/// so every player on the map hears it regardless of distance.
/// </summary>
[RegisterComponent]
public sealed partial class EmitGlobalSoundOnSpawnComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public AudioParams Params = AudioParams.Default;
}
