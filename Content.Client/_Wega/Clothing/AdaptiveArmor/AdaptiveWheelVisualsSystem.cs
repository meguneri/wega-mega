using Content.Shared._Wega.Clothing.AdaptiveArmor;
using Robust.Client.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client._Wega.Clothing.AdaptiveArmor;

/// <summary>
/// Visuals for the Mahoraga adaptation wheel. The ring, spokes and glow spin/pulse on their own (looping
/// RSI), so this system only drives the discrete state the server pushes: it colours each of the 8 glow
/// sectors by the currently-adapted damage types (sector i gets colour of activeTypes[i*N/8]), fills the
/// outer segment gauge as the plating learns more types, and pops a bright flash (eased back to each
/// sector's own colour) every time <see cref="AdaptiveWheelVisuals.Spin"/> ticks.
/// </summary>
public sealed partial class AdaptiveWheelVisualsSystem : VisualizerSystem<AdaptiveWheelComponent>
{
    private static readonly Color FlashColor = Color.White;
    private const int SectorCount = 8;

    protected override void OnAppearanceChange(EntityUid uid, AdaptiveWheelComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        // Recolour sectors whenever the active-types list changes (comma-separated string from server).
        if (AppearanceSystem.TryGetData<string>(uid, AdaptiveWheelVisuals.ActiveTypes, out var typesStr, args.Component))
        {
            var activeTypes = string.IsNullOrEmpty(typesStr)
                ? Array.Empty<string>()
                : typesStr.Split(',');

            if (activeTypes.Length == 0)
            {
                HideAllSectors((uid, args.Sprite));
            }
            else
            {
                for (var i = 0; i < SectorCount; i++)
                {
                    var typeIdx = i * activeTypes.Length / SectorCount;
                    comp.SectorColors[i] = AdaptiveArmorColors.ForType(activeTypes[typeIdx]);
                }
                // Keep TypeColor as dominant for any legacy callers.
                comp.TypeColor = comp.SectorColors[0];

                if (comp.FlashRemaining <= 0f)
                    ApplySectorColors((uid, args.Sprite), comp);
            }
        }

        // A bumped spin counter means a fresh adaptation: ratchet the spokes and pop a flash.
        if (AppearanceSystem.TryGetData<int>(uid, AdaptiveWheelVisuals.Spin, out var spin, args.Component)
            && spin != comp.LastSpin)
        {
            comp.LastSpin = spin;
            var strong = AppearanceSystem.TryGetData<bool>(uid, AdaptiveWheelVisuals.Strong, out var s, args.Component) && s;
            comp.FlashDuration = strong ? 0.5f : 0.3f;
            comp.FlashRemaining = comp.FlashDuration;
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
                for (var i = 0; i < SectorCount; i++)
                {
                    var layer = SectorLayer(i);
                    if (SpriteSystem.LayerMapTryGet((uid, sprite), layer, out var idx, false))
                        SpriteSystem.LayerSetColor((uid, sprite), idx, Color.InterpolateBetween(comp.SectorColors[i], FlashColor, t));
                }
                if (comp.FlashRemaining <= 0f)
                    ApplySectorColors(ent, comp);
            }

            if (comp.SpinRemaining > 0f)
            {
                comp.SpinRemaining -= frameTime;
                var p = comp.SpinDuration > 0f ? Math.Clamp(1f - comp.SpinRemaining / comp.SpinDuration, 0f, 1f) : 1f;
                var eased = 1f - MathF.Pow(1f - p, 3f);
                var angle = comp.SpinStartAngle + (comp.SpokeAngle - comp.SpinStartAngle) * eased;
                SetSpin(ent, comp.SpinRemaining > 0f ? angle : comp.SpokeAngle);
            }
        }
    }

    private void ApplySectorColors(Entity<SpriteComponent> ent, AdaptiveWheelComponent comp)
    {
        for (var i = 0; i < SectorCount; i++)
        {
            var layer = SectorLayer(i);
            if (!SpriteSystem.LayerMapTryGet((ent.Owner, ent.Comp), layer, out var idx, false))
                continue;
            SpriteSystem.LayerSetVisible((ent.Owner, ent.Comp), idx, true);
            SpriteSystem.LayerSetColor((ent.Owner, ent.Comp), idx, comp.SectorColors[i]);
        }
    }

    private void HideAllSectors(Entity<SpriteComponent> ent)
    {
        for (var i = 0; i < SectorCount; i++)
        {
            var layer = SectorLayer(i);
            if (SpriteSystem.LayerMapTryGet((ent.Owner, ent.Comp), layer, out var idx, false))
                SpriteSystem.LayerSetVisible((ent.Owner, ent.Comp), idx, false);
        }
    }

    private static AdaptiveWheelLayers SectorLayer(int i) => (AdaptiveWheelLayers)((int)AdaptiveWheelLayers.Sector0 + i);

    private void SetSpin(Entity<SpriteComponent> ent, float radians)
    {
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

/// <summary>Layer-map keys on the wheel sprite: the <see cref="Spokes"/> (spokes + the eight rings) that
/// ratchet on each adaptation; the static <see cref="Frame"/> (rim + hub); and eight 45° glow sectors
/// (<see cref="Sector0"/>–<see cref="Sector7"/>) that are individually coloured by the adapted damage types.</summary>
public enum AdaptiveWheelLayers : byte
{
    Spokes,
    Frame,
    Sector0,
    Sector1,
    Sector2,
    Sector3,
    Sector4,
    Sector5,
    Sector6,
    Sector7,
}
