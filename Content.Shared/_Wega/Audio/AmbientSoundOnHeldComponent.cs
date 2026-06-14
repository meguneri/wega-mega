using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Audio;

/// <summary>
/// Enables the entity's <see cref="Content.Shared.Audio.AmbientSoundComponent"/>
/// only while it is held in a hand, muting it again when dropped. Used by gear
/// that should "hum" only in active use, like the arena energy buckler.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AmbientSoundOnHeldComponent : Component;
