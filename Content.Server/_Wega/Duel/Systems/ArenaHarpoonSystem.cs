using Content.Server.Beam;
using Content.Shared._Wega.Duel;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;

namespace Content.Server._Wega.Duel.Systems;

/// <summary>
/// Арена-гарпун: крюк (<see cref="ArenaHarpoonProjectileComponent"/>) при первом контакте либо
/// притягивает попавшего моба к стрелку, либо дёргает самого стрелка к стене/конструкции. Притяжение
/// реализовано «броском» (<see cref="ThrowingSystem"/>) — предсказуемо и без возни с физическими
/// джойнтами. От стрелка к точке зацепа на миг рисуется трос-луч.
/// </summary>
public sealed partial class ArenaHarpoonSystem : EntitySystem
{
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private BeamSystem _beam = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArenaHarpoonProjectileComponent, StartCollideEvent>(OnHookCollide);
    }

    private void OnHookCollide(EntityUid uid, ArenaHarpoonProjectileComponent comp, ref StartCollideEvent args)
    {
        if (comp.Used)
            return;

        var other = args.OtherEntity;

        // Стрелок крюка — из снаряда. Без него непонятно, кого к кому тянуть.
        if (!TryComp<ProjectileComponent>(uid, out var projectile) || projectile.Shooter is not { } shooter)
            return;

        // Не цепляемся за самого стрелка и за оружие.
        if (other == shooter || other == projectile.Weapon)
            return;

        var isMob = HasComp<MobStateComponent>(other) && other != shooter;
        var isSolid = args.OtherBody.BodyType == BodyType.Static || Transform(other).Anchored;

        // Прочие динамические предметы (брошенный мусор и т.п.) — пропускаем, крюк летит дальше.
        if (!isMob && !isSolid)
            return;

        comp.Used = true;

        var shooterPos = _transform.GetMapCoordinates(shooter);

        if (isMob)
        {
            // Притягиваем цель к стрелку.
            var targetPos = _transform.GetMapCoordinates(other);
            if (targetPos.MapId == shooterPos.MapId)
            {
                var dir = shooterPos.Position - targetPos.Position;
                _throwing.TryThrow(other, dir, comp.PullSpeed, user: shooter, compensateFriction: true);
            }

            if (comp.Damage != null)
                _damageable.TryChangeDamage(other, comp.Damage, origin: shooter);
        }
        else
        {
            // Рывок стрелка к точке зацепа (стена/конструкция).
            var hookPos = _transform.GetMapCoordinates(uid);
            if (hookPos.MapId == shooterPos.MapId)
            {
                var dir = hookPos.Position - shooterPos.Position;
                _throwing.TryThrow(shooter, dir, comp.PullSpeed, user: shooter, compensateFriction: true);
            }
        }

        // Трос-луч от стрелка к точке зацепа и звук.
        _beam.TryCreateBeam(shooter, other, comp.RopeBeamProto);
        if (comp.HitSound != null)
            _audio.PlayPvs(new SoundPathSpecifier(comp.HitSound), shooter);

        QueueDel(uid);
    }
}
