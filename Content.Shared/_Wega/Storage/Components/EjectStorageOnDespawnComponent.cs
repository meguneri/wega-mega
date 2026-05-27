namespace Content.Shared._Wega.Storage.Components;

/// <summary>
/// When this entity despawns via <c>TimedDespawnComponent</c>, all contents of its
/// <c>EntityStorageComponent</c> are ejected into the world first — preventing players
/// or items inside from being deleted along with the container.
/// </summary>
[RegisterComponent]
public sealed partial class EjectStorageOnDespawnComponent : Component { }
