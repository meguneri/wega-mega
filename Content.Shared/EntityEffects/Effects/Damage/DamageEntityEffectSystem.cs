using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Damage;

/// <summary>
/// Deals a flat amount of a single damage type to the target, optionally gated by a required and/or
/// blacklisted component. Used by hazard tiles (lava, liquid plasma) to punish vehicles that drive
/// onto them while sparing e.g. boats.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DamageEntityEffectSystem : EntityEffectSystem<DamageableComponent, Damage>
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IComponentFactory _factory = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<Damage> args)
    {
        var effect = args.Effect;

        if (effect.RequiredComponent != null && !HasNamedComponent(entity, effect.RequiredComponent))
            return;

        if (effect.BlacklistedComponent != null && HasNamedComponent(entity, effect.BlacklistedComponent))
            return;

        var damageSpec = new DamageSpecifier { DamageDict = { { effect.DamageType, effect.Amount } } };
        damageSpec *= args.Scale;

        _damageable.TryChangeDamage(entity.AsNullable(), damageSpec, ignoreResistances: true, interruptsDoAfters: false);
    }

    private bool HasNamedComponent(EntityUid uid, string name)
    {
        return _factory.TryGetRegistration(name, out var reg) && HasComp(uid, reg.Type);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Damage : EntityEffectBase<Damage>
{
    /// <summary>If set, the effect only applies to entities that have this component.</summary>
    [DataField]
    public string? RequiredComponent;

    /// <summary>If set, the effect is skipped for entities that have this component.</summary>
    [DataField]
    public string? BlacklistedComponent;

    /// <summary>Damage type to apply.</summary>
    [DataField(required: true)]
    public ProtoId<DamageTypePrototype> DamageType;

    /// <summary>Flat amount of <see cref="DamageType"/> dealt (scaled by the effect scale).</summary>
    [DataField]
    public float Amount;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-damage",
            ("chance", Probability),
            ("kind", prototype.Index(DamageType).LocalizedName),
            ("amount", Amount));
}
