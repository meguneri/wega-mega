using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Magic.Telekinesis;

[RegisterComponent]
public sealed partial class TelekinesisHoldingComponent : Component
{
    [DataField]
    public EntityUid Target;

    [DataField]
    public EntProtoId ThrowActionProto = "ActionTelekinesisThrow";

    [DataField]
    public EntityUid? ThrowActionEntity;
}
