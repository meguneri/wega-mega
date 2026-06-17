using System.Linq;
using Content.Shared._Wega.Clothing.Sandevistan;
using Robust.Client.GameObjects;
using Robust.Shared.Spawners;
using DrawDepthEnum = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Wega.Sandevistan;

/// <summary>
/// Client visuals for Sandevistan afterimages: copies the source user's sprite onto the
/// trailing entity, tints it a glowing blue and pins it below the mob layer so the
/// moving user draws on top — the David Martinez blur trail. The ghost fades its alpha out over
/// its lifetime instead of popping out instantly.
/// </summary>
public sealed class SandevistanAfterimageSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanAfterimageComponent, ComponentStartup>(OnStartup);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // Fade each ghost's alpha toward 0 over its remaining lifetime (TimedDespawn counts down).
        var query = EntityQueryEnumerator<SandevistanAfterimageComponent, TimedDespawnComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var ghost, out var despawn, out var sprite))
        {
            var frac = ghost.FadeDuration > 0f ? Math.Clamp(despawn.Lifetime / ghost.FadeDuration, 0f, 1f) : 1f;
            _sprite.SetColor((uid, sprite), ghost.BaseColor.WithAlpha(ghost.BaseColor.A * frac));
        }
    }

    // Server-spawned (networked) ghosts arrive with SourceEntity already set, so the copy can run on
    // startup. Client-spawned ghosts set SourceEntity after creation and call ApplyVisuals directly.
    private void OnStartup(Entity<SandevistanAfterimageComponent> ent, ref ComponentStartup args)
    {
        ApplyVisuals(ent);
    }

    /// <summary>
    /// Copies the source user's sprite onto the afterimage and paints it as a bright, unshaded blue
    /// ghost. Safe to call once SourceEntity is set; no-ops if the source has no sprite.
    /// </summary>
    public void ApplyVisuals(Entity<SandevistanAfterimageComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent.Comp.SourceEntity, out var userSprite))
            return;

        var sprite = EnsureComp<SpriteComponent>(ent);
        _sprite.CopySprite((ent.Comp.SourceEntity, userSprite), (ent.Owner, sprite));
        _sprite.SetDrawDepth((ent.Owner, sprite), (int) DrawDepthEnum.DeadMobs);
        // Bright glowing blue, like David Martinez's Sandevistan trail; alpha keeps it ghostly and
        // is faded down over the ghost's lifetime in FrameUpdate.
        _sprite.SetColor((ent.Owner, sprite), ent.Comp.BaseColor);

        // Unshaded so the trail glows at full brightness regardless of the (often dark) arena
        // lighting — otherwise the ghosts come out almost black. Must be set per layer; PostShader
        // alone doesn't take the sprite out of the lighting pass.
        for (var i = 0; i < sprite.AllLayers.Count(); i++)
            sprite.LayerSetShader(i, "unshaded");

        sprite.PostShader = null;
        sprite.RenderOrder = 0;
        sprite.EnableDirectionOverride = true;
        sprite.DirectionOverride = ent.Comp.DirectionOverride;
    }
}
