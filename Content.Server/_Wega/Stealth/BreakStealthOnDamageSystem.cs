using Content.Shared._Wega.Stealth;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Server._Wega.Stealth;

/// <summary>
///     Deactivates the phase cloak (any <see cref="BreakStealthOnDamageComponent"/>
///     clothing with <see cref="ItemToggleComponent"/>) worn in the outer slot the moment its
///     wearer does anything combative — a melee attack, a shot or a throw — or takes damage.
///     The cloak stays on only while the infiltrator is passive; acting (or getting hit) reveals them.
/// </summary>
public sealed partial class BreakStealthOnDamageSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Зацепило уроном — раскрываемся.
        SubscribeLocalEvent<InventoryComponent, DamageChangedEvent>(OnWearerDamaged);

        // Любое наступательное действие носителя — раскрываемся:
        // удар (в т.ч. безоружный), выстрел, бросок гранаты/предмета.
        SubscribeLocalEvent<InventoryComponent, AttackAttemptEvent>(OnWearerAttack);
        SubscribeLocalEvent<InventoryComponent, ShotAttemptedEvent>(OnWearerShoot);
        SubscribeLocalEvent<InventoryComponent, BeforeThrowEvent>(OnWearerThrow);
    }

    private void OnWearerDamaged(Entity<InventoryComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        BreakCloak(ent.Owner, ent.Comp);
    }

    private void OnWearerAttack(EntityUid uid, InventoryComponent comp, AttackAttemptEvent args)
        => BreakCloak(uid, comp);

    private void OnWearerShoot(Entity<InventoryComponent> ent, ref ShotAttemptedEvent args)
        => BreakCloak(ent.Owner, ent.Comp);

    private void OnWearerThrow(Entity<InventoryComponent> ent, ref BeforeThrowEvent args)
        => BreakCloak(ent.Owner, ent.Comp);

    /// <summary>
    ///     Если в наружном слоте носителя надет активный фазовый покров
    ///     (<see cref="BreakStealthOnDamageComponent"/> + включённый <see cref="ItemToggleComponent"/>) —
    ///     выключает его. Иначе — ничего не делает.
    /// </summary>
    private void BreakCloak(EntityUid wearer, InventoryComponent inv)
    {
        if (!_inventory.TryGetSlotEntity(wearer, "outerClothing", out var suit, inv))
            return;

        if (!HasComp<BreakStealthOnDamageComponent>(suit))
            return;

        if (!TryComp<ItemToggleComponent>(suit, out var toggle) || !toggle.Activated)
            return;

        _toggle.TryDeactivate((suit.Value, toggle), wearer, predicted: false);
    }
}
