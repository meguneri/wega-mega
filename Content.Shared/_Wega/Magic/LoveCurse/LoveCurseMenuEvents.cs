using Robust.Shared.Serialization;

namespace Content.Shared._Wega.Magic.LoveCurse;

/// <summary>Server → client: open the target-picker panel for this caster.</summary>
[Serializable, NetSerializable]
public sealed class LoveCurseMenuOpenedEvent : EntityEventArgs
{
    public NetEntity Caster;

    public LoveCurseMenuOpenedEvent(NetEntity caster)
    {
        Caster = caster;
    }
}

/// <summary>Client → server: player picked a target from the panel.</summary>
[Serializable, NetSerializable]
public sealed class LoveCurseTargetSelectedEvent : EntityEventArgs
{
    public NetEntity Caster;
    public NetEntity Target;

    public LoveCurseTargetSelectedEvent(NetEntity caster, NetEntity target)
    {
        Caster = caster;
        Target = target;
    }
}
