using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Wega.Arena.Components;

/// <summary>
/// Placed on the map. Tracks kills inside the arena and announces the score
/// via global chat. Activates on DuelFight signal, resets on DuelReset signal.
/// </summary>
[RegisterComponent]
public sealed partial class DuelKillTrackerComponent : Component
{
    /// <summary>
    /// Radius around this entity in which deaths are counted.
    /// </summary>
    [DataField]
    public float Range = 100f;

    /// <summary>
    /// Whether the tracker is currently active (duel in progress).
    /// </summary>
    [DataField]
    public bool Active = false;

    /// <summary>
    /// Kill counts per player name. Cleared on reset.
    /// </summary>
    [DataField]
    public Dictionary<string, int> Kills = new();

    /// <summary>
    /// Sender name shown in chat announcements.
    /// </summary>
    [DataField]
    public string Sender = "Арена";

    /// <summary>
    /// Port name that activates the tracker.
    /// Timer source (DuelFight channel) maps to Open by default.
    /// </summary>
    [DataField]
    public string StartPort = "Open";

    /// <summary>
    /// Port name that deactivates the tracker.
    /// Pressed source (DuelReset channel) maps to Toggle by default.
    /// </summary>
    [DataField]
    public string ResetPort = "Toggle";
}
