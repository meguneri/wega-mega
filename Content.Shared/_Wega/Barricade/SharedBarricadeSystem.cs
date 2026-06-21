using Content.Shared.Projectiles;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared._Wega.Barricade;

/// <summary>
/// Перехват снарядов баррикадой-укрытием. Решает, пролетит ли снаряд сквозь баррикаду или
/// столкнётся с ней. Сам бросок монетки (с учётом дистанции) выполняется на сервере
/// (см. серверный <c>BarricadeSystem</c>) и кешируется в <see cref="PassBarricadeComponent"/>,
/// чтобы клиентское предсказание не расходилось с сервером.
/// </summary>
public abstract partial class SharedBarricadeSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BarricadeComponent, PreventCollideEvent>(OnPreventCollide);

        SubscribeLocalEvent<PassBarricadeComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<PassBarricadeComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<PassBarricadeComponent, EndCollideEvent>(OnEndCollide);
    }

    private void OnPreventCollide(Entity<BarricadeComponent> entity, ref PreventCollideEvent args)
    {
        if (args.Cancelled)
            return;

        // Сущности из вайтлиста проходят всегда.
        if (_whitelist.IsWhitelistPass(entity.Comp.Whitelist, args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        // Перехватываем только физические снаряды.
        if (TryComp<ProjectileComponent>(args.OtherEntity, out var projectile)
            && ProjectileTryPassBarricade(entity, (args.OtherEntity, projectile)))
        {
            args.Cancelled = true;
        }
    }

    // Снаряд перестал лететь (приземлился/попал) — кеш решений по баррикадам больше не нужен.
    private void OnLand(Entity<PassBarricadeComponent> entity, ref LandEvent args)
    {
        entity.Comp.CollideBarricades.Clear();
    }

    private void OnProjectileHit(Entity<PassBarricadeComponent> entity, ref ProjectileHitEvent args)
    {
        entity.Comp.CollideBarricades.Clear();
    }

    private void OnEndCollide(Entity<PassBarricadeComponent> entity, ref EndCollideEvent args)
    {
        if (HasComp<BarricadeComponent>(args.OtherEntity))
            entity.Comp.CollideBarricades.Remove(args.OtherEntity);
    }

    /// <summary>
    /// Проходит ли снаряд сквозь баррикаду. База (клиент) знает лишь то, что уже решил сервер и
    /// прислал в <see cref="PassBarricadeComponent.CollideBarricades"/>; сам бросок делает сервер.
    /// </summary>
    protected virtual bool ProjectileTryPassBarricade(Entity<BarricadeComponent> entity, Entity<ProjectileComponent> projEnt)
    {
        if (TryComp<PassBarricadeComponent>(projEnt.Owner, out var pass)
            && pass.CollideBarricades.TryGetValue(entity.Owner, out var isPass))
            return isPass;

        return false;
    }
}
