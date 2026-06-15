using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._Wega.Evasion;

/// <summary>
/// Rolls the dodge chance from <see cref="EvasionComponent"/> against incoming attacks and, on a
/// success, zeroes out the damage so the hit is fully negated. _Wega
/// </summary>
public sealed partial class EvasionSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EvasionComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    private void OnDamageModify(Entity<EvasionComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        // Only dodge actual incoming attacks (positive damage from some attacker), never healing or
        // anonymous environmental damage.
        if (args.Args.Origin == null || args.Args.Damage.GetTotal() <= 0)
            return;

        // The dodge is random, so it can't be predicted on the client — decide it on the server and
        // let the result replicate, otherwise we'd flicker the damage.
        if (!_net.IsServer || !_random.Prob(ent.Comp.Chance))
            return;

        var wearer = args.Owner;
        args.Args.Damage = new DamageSpecifier();
        _popup.PopupEntity(Loc.GetString("evasion-dodged"), wearer, wearer, PopupType.SmallCaution);
    }
}
