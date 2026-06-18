using System.Numerics;
using Content.Shared._Wega.Duel;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._Wega.Duel;

/// <summary>
/// Рисует «шторм» на дуэльной арене: опасная зона (всё за пределами безопасного круга — центр в
/// позиции трекера с <see cref="ArenaStormComponent"/>, радиус — <c>CurrentRadius</c>) затягивается
/// полупрозрачным красным полотном, которое «нарастает» по мере сужения зоны. Безопасная зона никак
/// не выделяется. По границе — тонкое пульсирующее красное кольцо как чёткий маркер края.
/// Самогейтится по активным штормам на карте глаза игрока.
/// </summary>
public sealed class ArenaStormOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SharedTransformSystem _transform;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ArenaStormOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transform = _entity.System<SharedTransformSystem>();
        ZIndex = 10;
    }

    /// <summary>Сегментов по окружности для заливки опасного кольца.</summary>
    private const int Segments = 96;

    /// <summary>Переиспользуемый буфер вершин triangle-strip (stackalloc запрещён песочницей).</summary>
    private readonly Vector2[] _ringVerts = new Vector2[(Segments + 1) * 2];

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        // Пульсация: 0..1, период ~1.2с.
        var t = (float)_timing.RealTime.TotalSeconds;
        var pulse = 0.5f + 0.5f * MathF.Sin(t * MathF.Tau / 1.2f);

        var query = _entity.EntityQueryEnumerator<ArenaStormComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var storm, out var xform))
        {
            if (!storm.Active || storm.CurrentRadius <= 0f || xform.MapID != args.MapId)
                continue;

            var center = _transform.GetWorldPosition(uid);
            var inner = storm.CurrentRadius;

            // Внешний радиус полотна — гарантированно за пределами видимой области, чтобы залить
            // всё опасное пространство до краёв экрана (берём дистанцию до дальнего угла + запас).
            var bounds = args.WorldBounds;
            var outer = MaxDistance(center, bounds) + 4f;
            if (outer <= inner)
                continue;

            DrawDangerRing(handle, center, inner, outer, new Color(0.85f, 0.1f, 0.08f, 0.28f));

            // Тонкое пульсирующее кольцо-маркер по самой границе.
            var ring = new Color(1f, 0.25f, 0.15f, 0.45f + 0.4f * pulse);
            handle.DrawCircle(center, inner, ring, false);
        }
    }

    /// <summary>
    /// Заливает кольцо [inner; outer] вокруг center сплошным цветом через triangle-strip — так
    /// безопасный круг остаётся «дырой» без заливки, а всё снаружи затягивается полотном.
    /// </summary>
    private void DrawDangerRing(DrawingHandleWorld handle, Vector2 center, float inner, float outer, Color color)
    {
        // 2 вершины на сегмент (внутренняя/внешняя) + замыкающая пара.
        for (var i = 0; i <= Segments; i++)
        {
            var ang = i / (float)Segments * MathF.Tau;
            var dir = new Vector2(MathF.Cos(ang), MathF.Sin(ang));
            _ringVerts[i * 2] = center + dir * inner;
            _ringVerts[i * 2 + 1] = center + dir * outer;
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, _ringVerts, color);
    }

    /// <summary>Максимальная дистанция от точки до углов прямоугольника обзора.</summary>
    private static float MaxDistance(Vector2 center, Box2Rotated bounds)
    {
        var max = 0f;
        max = MathF.Max(max, (bounds.BottomLeft - center).Length());
        max = MathF.Max(max, (bounds.BottomRight - center).Length());
        max = MathF.Max(max, (bounds.TopLeft - center).Length());
        max = MathF.Max(max, (bounds.TopRight - center).Length());
        return max;
    }
}
