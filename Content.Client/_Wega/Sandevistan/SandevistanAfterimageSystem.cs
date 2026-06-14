using Content.Shared._Wega.Clothing.Sandevistan;
using Robust.Client.GameObjects;
using DrawDepthEnum = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Wega.Sandevistan;

/// <summary>
/// Client visuals for Sandevistan afterimages: copies the source user's sprite onto the
/// trailing entity, tints it translucent blue and pins it below the mob layer so the
/// moving user draws on top — the David Martinez blur trail.
/// </summary>
public sealed class SandevistanAfterimageSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanAfterimageComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<SandevistanAfterimageComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent.Comp.SourceEntity, out var userSprite))
            return;

        var sprite = EnsureComp<SpriteComponent>(ent);
        _sprite.CopySprite((ent.Comp.SourceEntity, userSprite), (ent.Owner, sprite));
        _sprite.SetDrawDepth((ent.Owner, sprite), (int) DrawDepthEnum.DeadMobs);
        // Постоянный синеватый оттенок (как у Сандэвистана Дэвида Мартинеса), а не радужный цикл.
        _sprite.SetColor((ent.Owner, sprite), new Color(0.3f, 0.65f, 1f, 0.6f));

        sprite.PostShader = null;
        sprite.RenderOrder = 0;
        sprite.EnableDirectionOverride = true;
        sprite.DirectionOverride = ent.Comp.DirectionOverride;
    }
}
