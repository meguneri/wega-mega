using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Barricade;

/// <summary>
/// Объект-укрытие (мешки с песком, опрокинутый стол и т.п.), который ловит пролетающие сквозь
/// него снаряды с шансом, зависящим от дистанции выстрела: чем ближе стрелок к баррикаде, тем
/// легче снаряд проходит мимо неё; чем дальше — тем выше шанс, что баррикада его перехватит
/// (и снаряд попадёт по самой баррикаде, а не по тому, кто за ней).
///
/// Работает только для физических снарядов (<c>ProjectileComponent</c>); хитскан/лазеры не
/// перехватывает.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedBarricadeSystem))]
public sealed partial class BarricadeComponent : Component
{
    /// <summary>
    /// Шанс перехвата снаряда, если дистанция до стрелка ≤ <see cref="MinDistance"/>.
    /// </summary>
    [DataField]
    public float MinHitChance = 0f;

    /// <summary>
    /// Шанс перехвата снаряда, если дистанция до стрелка ≥ <see cref="MaxDistance"/>.
    /// </summary>
    [DataField]
    public float MaxHitChance = 0.75f;

    /// <summary>
    /// Дистанция, ближе которой шанс перехвата равен <see cref="MinHitChance"/>.
    /// </summary>
    [DataField]
    public float MinDistance = 1.5f;

    /// <summary>
    /// Дистанция, дальше которой шанс перехвата равен <see cref="MaxHitChance"/>.
    /// </summary>
    [DataField]
    public float MaxDistance = 12f;

    /// <summary>
    /// Сущности из этого вайтлиста всегда пролетают сквозь баррикаду без проверки шанса.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;
}
