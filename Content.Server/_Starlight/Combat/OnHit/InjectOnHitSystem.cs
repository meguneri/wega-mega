using System.Linq;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage.Components;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server._Starlight.Combat.OnHit;

/// <summary>
///     Injects reagents into entities hit by a melee weapon carrying
///     <see cref="InjectOnHitComponent"/>. Ported from lust-station / Starlight.
/// </summary>
public sealed partial class InjectOnHitSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ReactiveSystem _reactiveSystem = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainers = default!;
    [Dependency] private SharedColorFlashEffectSystem _color = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectOnHitComponent, MeleeHitEvent>(OnInjectOnMeleeHit);
    }

    private void OnInjectOnMeleeHit(Entity<InjectOnHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !args.HitEntities.Any())
            return;

        var ev = new InjectOnHitAttemptEvent();
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
            return;

        foreach (var target in args.HitEntities)
        {
            if (_solutionContainers.TryGetInjectableSolution(target, out var targetSoln, out _))
            {
                var solution = new Solution(ent.Comp.Reagents);

                foreach (var reagent in ent.Comp.Reagents)
                {
                    if (ent.Comp.ReagentLimit != null
                        && _solutionContainers.GetTotalPrototypeQuantity(target, reagent.Reagent.ToString()) >= FixedPoint2.New(ent.Comp.ReagentLimit.Value))
                        return;
                }

                _reactiveSystem.DoEntityReaction(target, solution, ReactionMethod.Injection);
                _solutionContainers.TryAddSolution(targetSoln.Value, solution);
                _color.RaiseEffect(Color.FromHex("#0000FF"), new List<EntityUid>(1) { target }, Filter.Pvs(target, entityManager: EntityManager));
            }

            if (ent.Comp.Sound is not null)
                _audio.PlayPvs(ent.Comp.Sound, target);
        }
    }
}
