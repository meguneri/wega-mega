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

    /// <summary>На каком расстоянии до якоря считаем, что «долетел», и завершаем подмотку (стан/потрошение
    /// для моба или рывок к стене для стрелка). Берётся с запасом (~тайл), чтобы сработать раньше, чем
    /// хитбоксы упрутся друг в друга, иначе стан не повесился бы.</summary>
    [DataField]
    public float ArriveDistance = 1.0f;

    /// <summary>Урон цели при зацепе (помимо притяжения).</summary>
    [DataField]
    public DamageSpecifier? Damage;

    /// <summary>Звук срабатывания зацепа.</summary>
    [DataField]
    public string? HitSound = "/Audio/Weapons/Guns/Gunshots/harpoon.ogg";

    /// <summary>Звук «прилёта»: глухой удар в момент, когда стрелок дёрнулся к стене вплотную или цель
    /// влетела к стрелку (кроме потрошащего финала — у него свой звук). null — без звука.</summary>
    [DataField]
    public string? LandSound = "/Audio/Effects/metal_slam1.ogg";

    /// <summary>Сколько секунд цель станит (paralyze), когда её подтянуло вплотную к стрелку.</summary>
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(4);

    /// <summary>Предохранитель: максимум времени на подмотку, после чего пул завершается принудительно.</summary>
    [DataField]
    public TimeSpan MaxPullTime = TimeSpan.FromSeconds(2);

    /// <summary>Что гарпун делает с притянутой вплотную жертвой по умолчанию. На потрошителе режим
    /// переключается в руке (<see cref="ArenaHarpoonModeComponent"/>) и переопределяет это значение.</summary>
    [DataField]
    public ArenaHarpoonFinisher Finisher = ArenaHarpoonFinisher.None;

    /// <summary>На каком расстоянии до якоря потрошитель начинает телеграфить добивание
    /// (нарастающие алые вспышки + тряска камеры + звук «завода»).</summary>
    [DataField]
    public float TelegraphDistance = 2.5f;

    /// <summary>Звук «завода» перед срывом конечности — нарастающий металлический скрежет.</summary>
    [DataField]
    public string? DismemberWindupSound = "/Audio/Weapons/chainsaw_rev.ogg";

    /// <summary>Звук самого отрыва конечности в упор.</summary>
    [DataField]
    public string? DismemberSound = "/Audio/Effects/gib3.ogg";

    /// <summary>Звук «завода» перед обезглавливанием — намеренно жутче скрежета потрошителя.</summary>
    [DataField]
    public string? BeheadWindupSound = "/Audio/Effects/demon_attack1.ogg";

    /// <summary>Звук самого обезглавливания в упор — самый страшный из добиваний гарпуна.</summary>
    [DataField]
    public string? BeheadSound = "/Audio/Effects/demon_consume.ogg";

    /// <summary>Звук, когда вместо головы гарпун вдребезги разбивает шлем-с-резистами (голова уцелела).</summary>
    [DataField]
    public string? HelmetBreakSound = "/Audio/Effects/metal_break1.ogg";

    /// <summary>Крюк уже сработал — защита от повторной обработки нескольких контактов за тик.</summary>
    public bool Used;
}
