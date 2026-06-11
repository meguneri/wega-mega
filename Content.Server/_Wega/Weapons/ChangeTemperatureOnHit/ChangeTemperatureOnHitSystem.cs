using Content.Server.Temperature.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Weapons.ChangeTemperatureOnHit;

public sealed partial class ChangeTemperatureOnHitSystem : EntitySystem
{
    [Dependency] private TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeTemperatureOnHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<ChangeTemperatureOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            _temperature.ChangeHeat(target, ent.Comp.Heat, ent.Comp.IgnoreResistances);
        }
    }
}
