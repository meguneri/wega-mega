namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// Automatically links with transmitters that share the same channel on map-init.
/// Supports a single channel via <see cref="AutoLinkChannel"/> (legacy) and multiple
/// channels via <see cref="AutoLinkChannels"/>. Both fields are checked independently.
/// </summary>
[RegisterComponent]
public sealed partial class AutoLinkReceiverComponent : Component
{
    /// <summary>
    /// Single-channel shorthand. Still used by most existing entities.
    /// </summary>
    [DataField("channel")]
    public string AutoLinkChannel = string.Empty;

    /// <summary>
    /// Additional channels. Use when one entity must respond to more than one transmitter channel.
    /// </summary>
    [DataField("channels")]
    public List<string> AutoLinkChannels = new();
}
