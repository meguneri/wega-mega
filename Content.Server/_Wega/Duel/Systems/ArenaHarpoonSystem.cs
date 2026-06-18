using System.Numerics;
using Content.Server.Effects;
using Content.Shared._Wega.Duel;
using Content.Shared.Body;
using Content.Shared.Camera;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Арена-гарпун: крюк (<see cref="ArenaHarpoonProjectileComponent"/>) при первом контакте либо
/// притягивает попавшего моба к стрелку, либо дёргает самого стрелка к стене/конструкции. Само
/// подтягивание идёт плавно каждый тик (<see cref="ArenaHarpoonPulledComponent"/>) — цель ровно
/// едет к якорю; трос (<see cref="ArenaHarpoonRopeComponent"/>) клиентский оверлей рисует между
/// концами по их живым позициям, поэтому он приклеен к модельке и плавно укорачивается. Притянутого
/// моба валит с ног на время подмотки, в момент попадания он получает урон, а долетев вплотную —
/// станится. «Потрошащая» версия по мере сближения нагнетает телеграф (вспышки + тряска + скрежет)
/// и в упор отрывает случайную конечность с финальной отдачей.
/// </summary>
public sealed partial class ArenaHarpoonSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private ColorFlashEffectSystem _color = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>Категории «органов»-конечностей, которые может оторвать потрошащий гарпун.</summary>
    private static readonly string[] LimbCategories =
    {
        "ArmLeft", "ArmRight", "HandLeft", "HandRight",
        "LegLeft", "LegRight", "FootLeft", "FootRight",
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArenaHarpoonProjectileComponent, StartCollideEvent>(OnHookCollide);
    }

    private void OnHookCollide(EntityUid uid, ArenaHarpoonProjectileComponent comp, ref StartCollideEvent args)
    {
        if (comp.Used)
            return;

        // Реагируем только на «боевой» хитбокс снаряда и только на твёрдые тела — как обычная пуля
        // (см. ProjectileSystem). Иначе срабатывает сенсорная fly-by-фикстура BaseBullet (радиус 1.5):
        // она цепляется за всё в полутора тайлах, включая стену за спиной стрелка, — и крюк «промахивался».
        if (args.OurFixtureId != SharedProjectileSystem.ProjectileFixture || !args.OtherFixture.Hard)
            return;

        var other = args.OtherEntity;

        // Стрелок крюка — из снаряда. Без него непонятно, кого к кому тянуть.
        if (!TryComp<ProjectileComponent>(uid, out var projectile) || projectile.Shooter is not { } shooter)
            return;

        // Не цепляемся за самого стрелка и за оружие.
        if (other == shooter || other == projectile.Weapon)
            return;

        var isMob = HasComp<MobStateComponent>(other);
        var isSolid = args.OtherBody.BodyType == BodyType.Static || Transform(other).Anchored;

        // Прочие динамические предметы (брошенный мусор и т.п.) — пропускаем, крюк летит дальше.
        if (!isMob && !isSolid)
            return;

        comp.Used = true;

        if (isMob)
        {
            // Урон по цели и плавная подмотка её к стрелку; вплотную — стан (и, возможно, потрошение).
            if (comp.Damage != null)
                _damageable.TryChangeDamage(other, comp.Damage, origin: shooter);

            // Валим с ног на время подмотки, чтобы цель ехала к стрелку, а не сопротивлялась ходьбой.
            _stun.TryKnockdown(other, comp.MaxPullTime, refresh: true);
            StartPull(other, anchor: shooter, comp, stunOnArrive: comp.StunDuration, dismember: comp.DismemberOnArrive);
        }
        else
        {
            // Рывок самого стрелка к точке зацепа (стена/конструкция). Без стана и потрошения.
            StartPull(shooter, anchor: other, comp, stunOnArrive: null, dismember: false);
        }

        if (comp.HitSound != null)
            _audio.PlayPvs(new SoundPathSpecifier(comp.HitSound), shooter);

        QueueDel(uid);
    }

    /// <summary>Навешивает на сущность состояние «подматывается гарпуном» к якорю-сущности
    /// <paramref name="anchor"/> (стрелок при притяжении моба или стена при рывке к ней).</summary>
    private void StartPull(EntityUid puller, EntityUid anchor, ArenaHarpoonProjectileComponent comp, TimeSpan? stunOnArrive, bool dismember)
    {
        var pull = EnsureComp<ArenaHarpoonPulledComponent>(puller);
        pull.Anchor = anchor;
        pull.AnchorPoint = _transform.GetMapCoordinates(anchor);
        pull.Speed = comp.PullSpeed;
        pull.StunOnArrive = stunOnArrive;
        pull.DismemberOnArrive = dismember;
        pull.WindupSound = comp.DismemberWindupSound;
        pull.DismemberSound = comp.DismemberSound;
        pull.EndTime = _timing.CurTime + comp.MaxPullTime;

        // Трос привязан к живому якорю — клиентский оверлей сам рисует его между концами по их
        // позициям каждый кадр, поэтому трос «приклеен» к модельке и плавно укорачивается.
        var rope = EnsureComp<ArenaHarpoonRopeComponent>(puller);
        rope.Anchor = GetNetEntity(anchor);
        Dirty(puller, rope);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ArenaHarpoonPulledComponent>();
        while (query.MoveNext(out var uid, out var pull))
        {
            // Текущая позиция якоря: живая сущность приоритетнее (тянет точно к ней, даже если она двигается),
            // иначе — зафиксированная точка.
            var anchorAlive = pull.Anchor is { } anchor && Exists(anchor);
            var anchorPos = anchorAlive
                ? _transform.GetMapCoordinates(pull.Anchor!.Value)
                : pull.AnchorPoint;

            var pullerPos = _transform.GetMapCoordinates(uid);

            // Предохранитель: вышло время или якорь на другой карте — завершаем без стана.
            if (_timing.CurTime >= pull.EndTime || pullerPos.MapId != anchorPos.MapId)
            {
                FinishPull(uid, pull, arrived: false);
                continue;
            }

            var delta = anchorPos.Position - pullerPos.Position;
            var dist = delta.Length();

            // Потрошитель: пока жертву ещё тянет, но она уже близко — заранее «заводимся».
            if (pull.DismemberOnArrive)
                UpdateDismemberTelegraph(uid, pull, dist);

            if (dist <= pull.ArriveDistance)
            {
                FinishPull(uid, pull, arrived: true);
                continue;
            }

            _physics.SetLinearVelocity(uid, delta / dist * pull.Speed);
        }
    }

    /// <summary>
    /// Телеграф потрошителя: когда подтягиваемая жертва входит в опасную дистанцию, запускается
    /// нарастающий «завод» — учащающиеся алые вспышки и всё более резкая тряска камеры, тем сильнее,
    /// чем ближе жертва. Так момент отрыва конечности читается заранее и нагнетает напряжение.
    /// </summary>
    private void UpdateDismemberTelegraph(EntityUid victim, ArenaHarpoonPulledComponent pull, float dist)
    {
        if (dist > pull.TelegraphDistance)
            return;

        if (!pull.TelegraphStarted)
        {
            pull.TelegraphStarted = true;
            if (pull.WindupSound != null)
                _audio.PlayPvs(new SoundPathSpecifier(pull.WindupSound), victim);
        }

        if (_timing.CurTime < pull.NextTelegraphTick)
            return;

        // 0 на краю опасной зоны → 1 вплотную. Чем ближе, тем чаще вспышки и сильнее тряска.
        var intensity = Math.Clamp(1f - dist / pull.TelegraphDistance, 0f, 1f);

        _color.RaiseEffect(Color.Red, new List<EntityUid> { victim }, Filter.Pvs(victim, entityManager: EntityManager));

        var kick = (0.25f + intensity * 0.75f);
        _recoil.KickCamera(victim, new Vector2(
            (_random.NextFloat() * 2f - 1f) * kick,
            (_random.NextFloat() * 2f - 1f) * kick));

        // Интервал между «дёргами»: 0.18с на краю → 0.05с вплотную.
        var interval = MathHelper.Lerp(0.18f, 0.05f, intensity);
        pull.NextTelegraphTick = _timing.CurTime + TimeSpan.FromSeconds(interval);
    }

    private void FinishPull(EntityUid uid, ArenaHarpoonPulledComponent pull, bool arrived)
    {
        if (TryComp<PhysicsComponent>(uid, out var phys))
            _physics.SetLinearVelocity(uid, Vector2.Zero, body: phys);

        if (arrived)
        {
            if (pull.StunOnArrive is { } stun)
                _stun.TryUpdateParalyzeDuration(uid, stun);

            if (pull.DismemberOnArrive)
                TryDismember(uid, pull);
        }

        // Трос больше не нужен — снимаем, оверлей сразу перестаёт его рисовать.
        RemCompDeferred<ArenaHarpoonRopeComponent>(uid);
        RemCompDeferred<ArenaHarpoonPulledComponent>(uid);
    }

    /// <summary>Отрывает у сущности одну случайную конечность (орган-лимб) и проигрывает финальную
    /// «потрошащую» отдачу — резкая алая вспышка, мощный толчок камеры и звук отрыва. Конечность
    /// выпадает на пол. Идёт сразу после нарастающего телеграфа, как кульминация.</summary>
    private void TryDismember(EntityUid victim, ArenaHarpoonPulledComponent pull)
    {
        if (!TryComp<BodyComponent>(victim, out var body) || body.Organs is not { } organs)
            return;

        var limbs = new List<EntityUid>();
        foreach (var organ in organs.ContainedEntities)
        {
            if (TryComp<OrganComponent>(organ, out var organComp)
                && organComp.Category is { } cat
                && Array.IndexOf(LimbCategories, cat.Id) >= 0)
            {
                limbs.Add(organ);
            }
        }

        if (limbs.Count == 0)
            return;

        var limb = _random.Pick(limbs);
        _container.Remove(limb, organs);

        // Кульминация: двойная алая вспышка + резкий толчок камеры + звук отрыва.
        var pvs = Filter.Pvs(victim, entityManager: EntityManager);
        _color.RaiseEffect(Color.Red, new List<EntityUid> { victim }, pvs);
        _recoil.KickCamera(victim, new Vector2(_random.NextFloat() * 2f - 1f, _random.NextFloat() * 2f - 1f) * 1.5f);

        if (pull.DismemberSound != null)
            _audio.PlayPvs(new SoundPathSpecifier(pull.DismemberSound), victim);
    }
}
