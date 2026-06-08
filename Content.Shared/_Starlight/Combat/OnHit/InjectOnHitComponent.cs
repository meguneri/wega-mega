using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

/// <summary>
///     Injects reagents into the target on melee hit. Ported from lust-station / Starlight.
/// </summary>
[RegisterComponent]
public sealed partial class InjectOnHitComponent : Component
{
    [DataField("reagents")]
    public List<ReagentQuantity> Reagents = new();

    [DataField("limit")]
    public float? ReagentLimit;

    [DataField("sound")]
    public SoundSpecifier? Sound;
}

[ByRefEvent]
public record struct InjectOnHitAttemptEvent(bool Cancelled);
