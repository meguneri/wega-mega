using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Duel;

/// <summary>
/// Снаряд-крюк арена-гарпуна. При первом столкновении сервер решает исход:
/// — попал в моба → притягивает цель к стрелку (бросок в сторону стрелка);
/// — попал в стену/статичную конструкцию → рывок самого стрелка к точке зацепа.
/// После срабатывания крюк исчезает. Лог обрабатывается в <c>ArenaHarpoonSystem</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ArenaHarpoonProjectileComponent : Component
{
    /// <summary>Скорость броска цели/стрелка (тайлов/с эквивалент throw speed).</summary>
    [DataField]
    public float PullSpeed = 12f;

    /// <summary>Урон цели при зацепе (помимо притяжения).</summary>
    [DataField]
    public DamageSpecifier? Damage;

    /// <summary>Прототип троса-луча (Beam), который рисуется от стрелка к точке зацепа.</summary>
    [DataField]
    public string RopeBeamProto = "ArenaHarpoonRope";

    /// <summary>Звук срабатывания зацепа.</summary>
    [DataField]
    public string? HitSound = "/Audio/Weapons/Guns/Gunshots/harpoon.ogg";

    /// <summary>Крюк уже сработал — защита от повторной обработки нескольких контактов за тик.</summary>
    public bool Used;
}
