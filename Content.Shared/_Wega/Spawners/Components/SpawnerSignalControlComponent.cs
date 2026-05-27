using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Wega.Spawners.Components;

/// <summary>
/// Marks an entity whose <see cref="Content.Server.Spawners.Components.TimedSpawnerComponent"/>
/// can be toggled on/off via device-link signals.
///
/// The actual logic runs in <c>SpawnerSignalControlSystem</c> (Content.Server).
/// This component lives in Content.Shared so the client can load entity prototypes
/// that reference it without crashing.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnerSignalControlComponent : Component
{
    /// <summary>
    /// Sink port name that toggles the spawner on/off when a signal is received.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string TogglePort = "Toggle";

    /// <summary>
    /// Sender name shown in the chat announcement (e.g. "Арена").
    /// </summary>
    [DataField]
    public string AnnounceSender = "Арена";

    /// <summary>
    /// Message broadcast when the spawner is turned ON.
    /// </summary>
    [DataField]
    public string EnabledMessage = "Сброс снаряжения активирован. Ящики будут появляться в центре арены каждые 60 секунд.";

    /// <summary>
    /// Message broadcast when the spawner is turned OFF.
    /// </summary>
    [DataField]
    public string DisabledMessage = "Сброс снаряжения остановлен.";
}
