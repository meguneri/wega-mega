using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Magic.LoveCurse;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LoveCurseComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;

    [DataField, AutoNetworkedField]
    public EntityUid? Caster;
}
