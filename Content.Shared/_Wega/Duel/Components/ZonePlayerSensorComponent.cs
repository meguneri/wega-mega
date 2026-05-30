using Robust.Shared.Timing;

namespace Content.Shared._Wega.Duel.Components;

/// <summary>
/// Sends High on DeviceLink when at least one alive mob is within Range.
/// Latches: once High, stays High until Reset signal received.
/// </summary>
[RegisterComponent]
public sealed partial class ZonePlayerSensorComponent : Component
{
    [DataField]
    public float Range = 1.5f;

    [DataField]
    public string OutputPort = "Output";

    [DataField]
    public string ResetPort = "Reset";

    [DataField]
    public TimeSpan CheckDelay = TimeSpan.FromSeconds(0.25);

    public TimeSpan NextCheck;
    public bool LastState;
}
