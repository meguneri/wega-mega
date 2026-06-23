using Content.Shared._Wega.Stealth;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;

namespace Content.Server._Wega.Stealth;

/// <summary>
///     Deactivates the phase cloak (any <see cref="BreakStealthOnDamageComponent"/>
///     clothing with <see cref="ItemToggleComponent"/>) on the wearer's outer slot
///     as soon as they take damage.
/// </summary>
public sealed partial class BreakStealthOnDamageSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, DamageChangedEvent>(OnWearerDamaged);
    }

    private void OnWearerDamaged(Entity<InventoryComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (!_inventory.TryGetSlotEntity(ent.Owner, "outerClothing", out var suit, ent.Comp))
            return;

        if (!HasComp<BreakStealthOnDamageComponent>(suit))
            return;

        if (!TryComp<ItemToggleComponent>(suit, out var toggle) || !toggle.Activated)
            return;

        _toggle.TryDeactivate((suit.Value, toggle), ent.Owner, predicted: false);
    }
}
