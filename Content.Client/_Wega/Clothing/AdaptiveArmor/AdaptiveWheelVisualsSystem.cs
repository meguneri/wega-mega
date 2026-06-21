using Content.Shared._Wega.Clothing.AdaptiveArmor;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client._Wega.Clothing.AdaptiveArmor;

/// <summary>
/// Visuals for the Mahoraga adaptation wheel. The ring, spokes and glow spin/pulse on their own (looping
/// RSI), so this system only drives the discrete state the server pushes: it tints the central glow by the
/// currently-adapted damage type, fills the outer segment gauge as the plating learns more types, and pops
/// a bright flash (eased back to the type colour) every time <see cref="AdaptiveWheelVisuals.Spin"/> ticks.
/// </summary>
public sealed partial class AdaptiveWheelVisualsSystem : VisualizerSystem<AdaptiveWheelComponent>
{
    /// <summary>The colour the glow flashes to on the instant of an adaptation, before easing back.</summary>
    private static readonly Color FlashColor = Color.White;

    protected override void OnAppearanceChange(EntityUid uid, AdaptiveWheelComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Tint the glow by the live adapted type (empty/idle falls back to the resting gold).
        if (AppearanceSystem.TryGetData<string>(uid, AdaptiveWheelVisuals.Type, out var type, args.Component))
        {
            comp.TypeColor = AdaptiveArmorColors.ForType(string.IsNullOrEmpty(type) ? null : type);
            if (comp.FlashRemaining <= 0f)
                SetGlow((uid, args.Sprite), comp.TypeColor);
        }

        // A bumped spin counter means a fresh adaptation: turn the wheel one notch and pop a flash
        // (both longer/brighter when the blow was actually absorbed).
        if (AppearanceSystem.TryGetData<int>(uid, AdaptiveWheelVisuals.Spin, out var spin, args.Component)
            && spin != comp.LastSpin)
        {
            comp.LastSpin = spin;
            var strong = AppearanceSystem.TryGetData<bool>(uid, AdaptiveWheelVisuals.Strong, out var s, args.Component) && s;
            comp.FlashDuration = strong ? 0.5f : 0.3f;
            comp.FlashRemaining = comp.FlashDuration;
            // Ratchet the spokes one 45° notch and hold there (no full revolution, no spring-back).
            comp.SpinStartAngle = comp.SpokeAngle;
            comp.SpokeAngle += MathF.PI / 4f;
            comp.SpinDuration = strong ? 0.4f : 0.32f;
            comp.SpinRemaining = comp.SpinDuration;
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<AdaptiveWheelComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var comp, out var sprite))
        {
            if (comp.FlashRemaining <= 0f && comp.SpinRemaining <= 0f)
                continue;

            var ent = (uid, sprite);

            if (comp.FlashRemaining > 0f)
            {
                comp.FlashRemaining -= frameTime;
                var t = comp.FlashDuration > 0f ? Math.Clamp(comp.FlashRemaining / comp.FlashDuration, 0f, 1f) : 0f;
                SetGlow(ent, Color.InterpolateBetween(comp.TypeColor, FlashColor, t));
                if (comp.FlashRemaining <= 0f)
                    SetGlow(ent, comp.TypeColor);
            }

            if (comp.SpinRemaining > 0f)
            {
                comp.SpinRemaining -= frameTime;
                // Ease the spokes from the previous notch into the new one (ease-out cubic) and settle.
                var p = comp.SpinDuration > 0f ? Math.Clamp(1f - comp.SpinRemaining / comp.SpinDuration, 0f, 1f) : 1f;
                var eased = 1f - MathF.Pow(1f - p, 3f);
                var angle = comp.SpinStartAngle + (comp.SpokeAngle - comp.SpinStartAngle) * eased;
                SetSpin(ent, comp.SpinRemaining > 0f ? angle : comp.SpokeAngle);
            }
        }
    }

    private void SetGlow(Entity<SpriteComponent> ent, Color color)
    {
        if (SpriteSystem.LayerMapTryGet((ent.Owner, ent.Comp), AdaptiveWheelLayers.Glow, out var layer, false))
            SpriteSystem.LayerSetColor((ent.Owner, ent.Comp), layer, color);
    }

    private void SetSpin(Entity<SpriteComponent> ent, float radians)
    {
        // The spokes (with their rings) turn; the rim/hub (Frame) and glow stay put.
        if (SpriteSystem.LayerMapTryGet((ent.Owner, ent.Comp), AdaptiveWheelLayers.Spokes, out var spokes, false))
            SpriteSystem.LayerSetRotation((ent.Owner, ent.Comp), spokes, new Angle(radians));
    }
}

/// <summary>Tints the one-shot absorb shockwave by the absorbed damage type the server stamped on it.</summary>
public sealed partial class AdaptiveShockwaveVisualsSystem : VisualizerSystem<AdaptiveShockwaveComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, AdaptiveShockwaveComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, AdaptiveWheelVisuals.Type, out var type, args.Component))
            SpriteSystem.LayerSetColor((uid, args.Sprite), 0, AdaptiveArmorColors.ForType(string.IsNullOrEmpty(type) ? null : type));
    }
}

/// <summary>Layer-map keys on the wheel sprite: the static type-tinted <see cref="Glow"/> behind; the
/// <see cref="Spokes"/> (spokes + the eight rings, all lit) — the moving part, ratcheted in code; and the
/// static <see cref="Frame"/> (rim + hub) they turn within.</summary>
public enum AdaptiveWheelLayers : byte
{
    Glow,
    Spokes,
    Frame,
}
