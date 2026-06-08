using System.Numerics;
using Content.Shared._RMC14.Explosion;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Random;

namespace Content.Client._RMC14._Sunrise.Explosion;

// Анимирует дым после взрыва: раздувание (Offset) + затухание (Color alpha).
// Перенос из lust-station / RMC14 (Sunrise).
public sealed class RMCExplosionSmokeSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string SmokeTrack = "smoke-animation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExplosionSmokeEffectComponent, ComponentStartup>(OnSmokeStartup);
        SubscribeLocalEvent<ExplosionEffectComponent, ComponentStartup>(OnExplosionStartup);
    }

    private const string ExplosionTrack = "explosion-animation";

    private void OnExplosionStartup(Entity<ExplosionEffectComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.SetScale((ent, sprite), new Vector2(ent.Comp.SizeModifier));
        // Отключаем авто-цикл RSI и проигрываем кадры ОДИН раз через SpriteFlick —
        // иначе анимация зацикливается/дёргается на деспавне.
        _sprite.LayerSetAutoAnimated((ent.Owner, sprite), "base", false);

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(ent.Comp.LifeTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = "base",
                    KeyFrames = { new AnimationTrackSpriteFlick.KeyFrame("grenade", 0f) },
                },
            },
        };

        _player.Play(ent.Owner, animation, ExplosionTrack);
    }

    private void OnSmokeStartup(Entity<ExplosionSmokeEffectComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var targetX = 2f + _random.NextFloat(-ExplosionSmokeEffectComponent.Variation, ExplosionSmokeEffectComponent.Variation);
        var targetY = 2f + _random.NextFloat(-ExplosionSmokeEffectComponent.Variation, ExplosionSmokeEffectComponent.Variation);

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(ent.Comp.LifeTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    Property = nameof(SpriteComponent.Offset),
                    ComponentType = typeof(SpriteComponent),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Offset, 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(targetX, targetY), ent.Comp.LifeTime),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    Property = nameof(SpriteComponent.Color),
                    ComponentType = typeof(SpriteComponent),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, 0f),
                        new AnimationTrackProperty.KeyFrame(GetTransparentColor(sprite.Color), ent.Comp.LifeTime),
                    },
                },
            },
        };

        _player.Play(ent, animation, SmokeTrack);
    }

    private static Color GetTransparentColor(Color color)
    {
        return new Color(color.R, color.G, color.B, 0f);
    }
}
