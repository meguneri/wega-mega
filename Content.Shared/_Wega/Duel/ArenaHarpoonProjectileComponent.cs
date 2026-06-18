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

    /// <summary>Звук срабатывания зацепа.</summary>
    [DataField]
    public string? HitSound = "/Audio/Weapons/Guns/Gunshots/harpoon.ogg";

    /// <summary>Сколько секунд цель станит (paralyze), когда её подтянуло вплотную к стрелку.</summary>
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(4);

    /// <summary>Предохранитель: максимум времени на подмотку, после чего пул завершается принудительно.</summary>
    [DataField]
    public TimeSpan MaxPullTime = TimeSpan.FromSeconds(2);

    /// <summary>Если true — когда притянутый моб оказывается вплотную, проигрывается короткая
    /// «потрошащая» анимация и у него отрывается одна случайная конечность.</summary>
    [DataField]
    public bool DismemberOnArrive;

    /// <summary>Звук «завода» потрошителя: запускается, когда жертва уже близко и вот-вот лишится
    /// конечности — нарастающий металлический скрежет под учащающиеся вспышки телеграфа.</summary>
    [DataField]
    public string? DismemberWindupSound = "/Audio/Weapons/chainsaw_rev.ogg";

    /// <summary>Звук самого отрыва конечности в упор.</summary>
    [DataField]
    public string? DismemberSound = "/Audio/Effects/gib3.ogg";

    /// <summary>Крюк уже сработал — защита от повторной обработки нескольких контактов за тик.</summary>
    public bool Used;
}
