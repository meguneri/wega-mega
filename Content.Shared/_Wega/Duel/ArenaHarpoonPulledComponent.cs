using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared._Wega.Duel;

/// <summary>
/// Помечает сущность, которую сейчас «подматывает» арена-гарпун: каждый тик её тянет к якорю
/// (стрелку при попадании в моба, или к точке зацепа на стене при рывке самого стрелка). При
/// достижении якоря пул завершается; для притянутого моба в этот момент вешается стан.
/// Компонент чисто рантаймовый — навешивается из <c>ArenaHarpoonSystem</c>, на карте не хранится.
/// </summary>
[RegisterComponent]
public sealed partial class ArenaHarpoonPulledComponent : Component
{
    /// <summary>Якорь-сущность, к которой тянем (обычно стрелок). Позиция пересчитывается каждый тик,
    /// чтобы тянуло точно к нему, даже если он двигается. Если null/исчез — используется <see cref="AnchorPoint"/>.</summary>
    public EntityUid? Anchor;

    /// <summary>Фиксированная точка-якорь (для рывка к стене) и запасной вариант, если <see cref="Anchor"/> пропал.</summary>
    public MapCoordinates AnchorPoint;

    /// <summary>Скорость подмотки (тайлов/с).</summary>
    public float Speed = 12f;

    /// <summary>На каком расстоянии до якоря считаем, что «долетел», и завершаем пул. Берём с запасом
    /// (~тайл), чтобы сработало раньше, чем хитбоксы упрутся друг в друга, иначе стан не повесился бы.</summary>
    public float ArriveDistance = 1.0f;

    /// <summary>Если задано — при достижении якоря притянутого станит (paralyze) на это время.</summary>
    public TimeSpan? StunOnArrive;

    /// <summary>Если true — при достижении якоря оторвать притянутому одну случайную конечность.</summary>
    public bool DismemberOnArrive;

    /// <summary>Предохранитель: после этого момента пул завершается принудительно (цель застряла и т.п.).</summary>
    public TimeSpan EndTime;

    // ── Телеграф потрошителя ──────────────────────────────────────────────────
    // Пока жертву ещё только подтягивает, но она уже близко к стрелку, потрошащий гарпун заранее
    // «заводится»: учащающиеся алые вспышки + тряска камеры + нарастающий звук пилы. Так момент
    // отрыва конечности читается заранее и выглядит куда эффектнее простой вспышки в упор.

    /// <summary>На каком расстоянии до якоря потрошитель начинает телеграфить отрыв конечности.</summary>
    public float TelegraphDistance = 2.5f;

    /// <summary>Телеграф уже запущен (звук подведён один раз).</summary>
    public bool TelegraphStarted;

    /// <summary>Когда в следующий раз дёрнуть вспышку/тряску телеграфа.</summary>
    public TimeSpan NextTelegraphTick;

    /// <summary>Звук «завода» потрошителя (нарастающий скрежет перед отрывом конечности).</summary>
    public string? WindupSound;

    /// <summary>Звук самого отрыва конечности.</summary>
    public string? DismemberSound;
}
