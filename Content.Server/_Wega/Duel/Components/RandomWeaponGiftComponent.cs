using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.Duel.Components;

/// <summary>
///     Подарочная коробка-рулетка: на MapInit выбирает случайное оружие из ВСЕХ
///     прототипов игры (любая сущность-предмет с GunComponent либо с MeleeWeaponComponent,
///     чей урон не ниже <see cref="MinMeleeDamage"/>), а при использовании в руке
///     распаковывается, как новогодний подарок. Пул строится динамически, ручной список не нужен.
/// </summary>
[RegisterComponent]
public sealed partial class RandomWeaponGiftComponent : Component
{
    /// <summary>
    ///     Выбранное при спавне оружие — выдаётся при распаковке.
    /// </summary>
    [DataField]
    public EntProtoId? SelectedEntity;

    /// <summary>
    ///     Минимальный суммарный урон ближнего боя, чтобы предмет без огнестрела
    ///     считался «оружием» (отсекает ручки, игрушки и прочий мусор с MeleeWeapon).
    /// </summary>
    [DataField]
    public double MinMeleeDamage = 5.0;

    /// <summary>
    ///     Обёртка, остающаяся после распаковки (мусор от подарка).
    /// </summary>
    [DataField]
    public EntProtoId? Wrapper;

    /// <summary>
    ///     Звук распаковки.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    ///     Кто видит содержимое при осмотре (призраки и т.п.).
    /// </summary>
    [DataField]
    public EntityWhitelist? ContentsViewers;

    /// <summary>
    ///     Помечать выданное как аренное (ArenaIssuedItemComponent), чтобы
    ///     уборка арены удаляла только выданное подарком.
    /// </summary>
    [DataField]
    public bool MarkIssuedItems = true;
}
