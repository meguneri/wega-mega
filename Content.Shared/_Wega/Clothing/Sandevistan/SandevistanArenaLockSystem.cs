using System.Linq;
using Content.Shared.Armor;
using Content.Shared.Blocking;
using Content.Shared.Clothing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Medical.Healing;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._Wega.Clothing.Sandevistan;

/// <summary>
/// The arena Sandevistan's weapon lock: while the eyewear (<see cref="SandevistanArenaLockComponent"/>)
/// is worn, the wearer may only attack with weapons flagged <see cref="ArenaAllowedWeaponComponent"/>
/// (the gloves of the north star). Any other melee weapon and all guns are blocked; unarmed attacks
/// remain allowed.
/// </summary>
public sealed partial class SandevistanArenaLockSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanArenaLockComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<SandevistanArenaLockComponent, ClothingGotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<ArenaWeaponLockComponent, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<ArenaWeaponLockComponent, ShotAttemptedEvent>(OnShotAttempt);
        SubscribeLocalEvent<ArenaWeaponLockComponent, IsEquippingTargetAttemptEvent>(OnEquipTargetAttempt);
        SubscribeLocalEvent<ArenaWeaponLockComponent, PickupAttemptEvent>(OnPickupAttempt);

        SubscribeLocalEvent<ArenaAllowedWeaponComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
    }

    /// <summary>
    /// Оружие (melee/огнестрел) и щиты нельзя взять в руки, пока надет арена-сандэвистан — попытка
    /// подбора отменяется. Перчатки полярной звезды (флаг <see cref="ArenaAllowedWeaponComponent"/>)
    /// надеваются в слот перчаток, а не в руки, так что их это не касается.
    /// </summary>
    private void OnPickupAttempt(Entity<ArenaWeaponLockComponent> ent, ref PickupAttemptEvent args)
    {
        if (IsHandLocked(args.Item))
            args.Cancel();
    }

    /// <summary>Оружие или щит, не помеченные как разрешённые — такое в руках держать нельзя.</summary>
    private bool IsHandLocked(EntityUid item)
    {
        if (HasComp<ArenaAllowedWeaponComponent>(item))
            return false;

        // Лекарства, расходники и еда — не оружие, держать можно (бутылки/пилюли/напитки имеют
        // MeleeWeaponComponent, т.к. ими «можно ударить», но это не повод запрещать лечиться).
        if (HasComp<EdibleComponent>(item) || HasComp<HealingComponent>(item))
            return false;

        return HasComp<MeleeWeaponComponent>(item)
            || HasComp<GunComponent>(item)
            || HasComp<BlockingComponent>(item);
    }

    /// <summary>
    /// Пока надет арена-сандэвистан, носитель не может надеть элементы брони (с <c>Armor</c>) и
    /// ускоряющую обувь (<c>ClothingSpeedModifier</c> с бустом &gt; 1).
    /// </summary>
    private void OnEquipTargetAttempt(Entity<ArenaWeaponLockComponent> ent, ref IsEquippingTargetAttemptEvent args)
    {
        var item = args.Equipment;

        var isArmor = HasComp<ArmorComponent>(item);
        var isSpeedBoost = TryComp<ClothingSpeedModifierComponent>(item, out var speed)
            && (speed.WalkModifier > 1f || speed.SprintModifier > 1f);

        if (!isArmor && !isSpeedBoost)
            return;

        args.Reason = "sandevistan-arena-gear-locked";
        args.Cancel();
    }

    /// <summary>
    /// Перчатки полярной звезды бьют сильнее (до 15), но только если на носителе именно
    /// арена-версия сандэвистана (т.е. активен <see cref="ArenaWeaponLockComponent"/>).
    /// </summary>
    private void OnGetMeleeDamage(Entity<ArenaAllowedWeaponComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (!HasComp<ArenaWeaponLockComponent>(args.User))
            return;

        args.Damage.DamageDict["Blunt"] = 15;
    }

    private void OnEquipped(Entity<SandevistanArenaLockComponent> ent, ref ClothingGotEquippedEvent args)
    {
        EnsureComp<ArenaWeaponLockComponent>(args.Wearer);

        // Уже зажатое оружие/щиты сразу выпадают из рук.
        foreach (var held in _hands.EnumerateHeld(args.Wearer).ToList())
        {
            if (IsHandLocked(held))
                _hands.TryDrop(args.Wearer, held, checkActionBlocker: false);
        }
    }

    private void OnUnequipped(Entity<SandevistanArenaLockComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemComp<ArenaWeaponLockComponent>(args.Wearer);
    }

    private void OnAttackAttempt(Entity<ArenaWeaponLockComponent> ent, ref AttackAttemptEvent args)
    {
        // Unarmed attacks (no weapon, or the weapon is the user's own body) stay allowed.
        if (args.Weapon is not { } weapon || weapon.Owner == ent.Owner)
            return;

        // Only flagged weapons (the gloves of the north star) are permitted.
        if (HasComp<ArenaAllowedWeaponComponent>(weapon.Owner))
            return;

        _popup.PopupClient(Loc.GetString("sandevistan-arena-weapon-locked"), ent, ent);
        args.Cancel();
    }

    private void OnShotAttempt(Entity<ArenaWeaponLockComponent> ent, ref ShotAttemptedEvent args)
    {
        // Guns are never allowed under the lock — only the flagged melee gloves.
        if (HasComp<ArenaAllowedWeaponComponent>(args.Used.Owner))
            return;

        _popup.PopupClient(Loc.GetString("sandevistan-arena-weapon-locked"), ent, ent);
        args.Cancel();
    }
}
