using System.Numerics;
using Content.Shared._Wega.Aphrodisiac;
using Content.Shared.CCVar;
using Content.Shared.StatusEffectNew;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Wega.Aphrodisiac;

/// <summary>
///     Оверлей любовного опьянения: розовый шейдер + пульсирующая виньетка + всплывающие
///     сердечки. Портировано из lust-station, адаптировано под новую систему
///     статус-эффектов (сила эффекта считается от оставшегося времени, как у RainbowOverlay).
/// </summary>
public sealed partial class LoveVisionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> LoveShader = "LoveVision";
    private static readonly ProtoId<ShaderPrototype> GradientShader = "GradientCircleMask";

    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IEntitySystemManager _sysMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly StatusEffectsSystem _statusEffects;
    private readonly SpriteSystem _sprite;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;

    private readonly ShaderInstance _loveVisionShader;
    private readonly ShaderInstance _gradient;
    private readonly Texture _heartTexture;
    private const string HeartTexturePath = "/Textures/_Wega/Interface/LoveVision/hearts.png";

    private const float RiseDistance = 100f;
    private const float RampUpSeconds = 5f;
    private const float FadeOutSeconds = 10f;
    private const float HeartsLifetime = 1.5f;

    private readonly Vector3 _gradientColor = new(1.0f, 0.3f, 0.7f); // Розово-фиолетовый
    private readonly List<HeartData> _hearts = [];

    private struct HeartData
    {
        public Vector2 Position;
        public TimeSpan SpawnTime;
        public Vector2 BaseSize;
    }

    private float _strength;
    private float _elapsed;
    private TimeSpan _nextHeartTime;

    public LoveVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        _statusEffects = _sysMan.GetEntitySystem<StatusEffectsSystem>();
        _sprite = _entityManager.System<SpriteSystem>();
        _loveVisionShader = _prototypeManager.Index(LoveShader).InstanceUnique();
        _gradient = _prototypeManager.Index(GradientShader).InstanceUnique();
        _heartTexture = _sprite.Frame0(new SpriteSpecifier.Texture(new ResPath(HeartTexturePath)));
    }

    public void Reset()
    {
        _strength = 0f;
        _elapsed = 0f;
        _hearts.Clear();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;

        if (playerEntity == null
            || !_statusEffects.TryGetEffectsEndTimeWithComp<LoveVisionStatusEffectComponent>(playerEntity, out var endTime))
        {
            Reset();
            return;
        }

        _elapsed += args.DeltaSeconds;

        endTime ??= TimeSpan.MaxValue;
        var timeLeft = (float)(endTime.Value - _timing.CurTime).TotalSeconds;

        // Плавный набор силы в начале и затухание к концу действия.
        var rampUp = Math.Clamp(_elapsed / RampUpSeconds, 0f, 1f);
        var fadeOut = Math.Clamp(timeLeft / FadeOutSeconds, 0f, 1f);
        _strength = Math.Min(rampUp, fadeOut);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComponent))
            return false;

        if (args.Viewport.Eye != eyeComponent.Eye)
            return false;

        return _strength > 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_config.GetCVar(CCVars.ReducedMotion))
            return;

        switch (args.Space)
        {
            case OverlaySpace.ScreenSpace:
                DrawScreen(args);
                break;
            case OverlaySpace.WorldSpace:
                DrawWorld(args);
                break;
        }
    }

    private void DrawScreen(in OverlayDrawArgs args)
    {
        if (_strength < 0.5f)
            return;

        var curTime = _timing.CurTime;

        if (curTime >= _nextHeartTime)
        {
            // Случайное изменение размера
            var baseSize = new Vector2(100, 100);
            var scale = _random.NextFloat(0.8f, 1.2f);
            var finalSize = baseSize * scale;

            _hearts.Add(new HeartData
            {
                Position = GetRandomSpawnPosition(args, 600f, 600f),
                SpawnTime = curTime,
                BaseSize = finalSize
            });

            // Задержка перед появлением нового сердечка
            var delay = _random.NextFloat() * 0.2f + 0.1;
            _nextHeartTime = curTime + TimeSpan.FromSeconds(delay);
        }

        var screen = args.ScreenHandle;

        for (var i = _hearts.Count - 1; i >= 0; i--)
        {
            var heart = _hearts[i];
            var timeElapsed = curTime - heart.SpawnTime;

            if (timeElapsed > TimeSpan.FromSeconds(HeartsLifetime))
            {
                _hearts.RemoveAt(i);
                continue;
            }

            var progress = timeElapsed / TimeSpan.FromSeconds(HeartsLifetime);

            // Сердечко поднимается вверх
            var offset = new Vector2(0, (float)-progress * RiseDistance);
            var position = heart.Position + offset;

            // Пульсация по синусоиде
            var pulse = 1f + 0.1f * MathF.Sin((float)timeElapsed.TotalSeconds * MathF.PI * 2f);
            var pulsingSize = heart.BaseSize * pulse;

            // Постепенная прозрачность
            var alpha = 1.0f - (float)progress;
            var modulate = new Color(255, 100, 180, (byte)(alpha * 255));

            var drawBox = new UIBox2(position, position + pulsingSize);

            screen.DrawTextureRect(_heartTexture, drawBox, modulate);
        }
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        var time = (float)_timing.RealTime.TotalSeconds;
        var distance = args.ViewportBounds.Width;

        _loveVisionShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _loveVisionShader.SetParameter("effectStrength", _strength);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(_loveVisionShader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);

        if (_strength > 0f)
        {
            var level = _strength / 2f;
            var pulseRate = 6f;
            var adjustedTime = time * pulseRate;

            var outerMaxLevel = 2.0f * distance;
            var outerMinLevel = 0.8f * distance;
            var innerMaxLevel = 0.6f * distance;
            var innerMinLevel = 0.2f * distance;

            var outerRadius = outerMaxLevel - level * (outerMaxLevel - outerMinLevel);
            var innerRadius = innerMaxLevel - level * (innerMaxLevel - innerMinLevel);

            var pulse = MathF.Max(0f, MathF.Sin(adjustedTime));

            _gradient.SetParameter("time", pulse);
            _gradient.SetParameter("color", _gradientColor);
            _gradient.SetParameter("darknessAlphaOuter", 2f);

            _gradient.SetParameter("outerCircleRadius", outerRadius);
            _gradient.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
            _gradient.SetParameter("innerCircleRadius", innerRadius);
            _gradient.SetParameter("innerCircleMaxRadius", innerRadius + 0.02f * distance);

            worldHandle.UseShader(_gradient);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(null);
        }
    }

    /// <summary>
    ///     Случайная позиция для сердечка вне центральной зоны экрана,
    ///     чтобы не загораживать персонажа.
    /// </summary>
    private Vector2 GetRandomSpawnPosition(in OverlayDrawArgs args, float width, float height)
    {
        var viewport = args.Viewport;
        var screenWidth = viewport.Size.X;
        var screenHeight = viewport.Size.Y;

        var centerX = screenWidth / 2f;
        var centerY = screenHeight / 2f;

        var exclusionRect = new Box2(
            new Vector2(centerX - width / 2f, centerY - height / 2f),
            new Vector2(centerX + width / 2f, centerY + height / 2f)
        );

        Vector2 randomPos;

        var maxTries = 20;
        var tries = 0;

        do
        {
            randomPos = new Vector2(
                _random.NextFloat(0, screenWidth),
                _random.NextFloat(0, screenHeight)
            );
            tries++;
        }
        while (exclusionRect.Contains(randomPos) && tries < maxTries);

        if (exclusionRect.Contains(randomPos))
        {
            randomPos = new Vector2(
                _random.Next(2) == 0
                    ? _random.NextFloat(0, exclusionRect.Left - 50f)
                    : _random.NextFloat(exclusionRect.Right + 50f, screenWidth),
                _random.NextFloat(0, screenHeight)
            );
        }

        return randomPos;
    }
}
