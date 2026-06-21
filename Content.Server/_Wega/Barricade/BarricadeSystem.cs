using System;
using Content.Shared._Wega.Barricade;
using Content.Shared.Projectiles;
using Robust.Shared.Random;

namespace Content.Server._Wega.Barricade;

/// <summary>
/// Серверная часть баррикады: бросает монетку «перехватить / пропустить» снаряд с шансом по
/// дистанции от баррикады до стрелка и кеширует результат в <see cref="PassBarricadeComponent"/>,
/// откуда его подхватывает клиент (чтобы предсказание не расходилось).
/// </summary>
public sealed partial class BarricadeSystem : SharedBarricadeSystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IRobustRandom _random = default!;

    protected override bool ProjectileTryPassBarricade(Entity<BarricadeComponent> entity, Entity<ProjectileComponent> projEnt)
    {
        var (projUid, projComp) = projEnt;

        var pass = EnsureComp<PassBarricadeComponent>(projUid);

        // По этой баррикаде решение уже принято — держимся его (стабильность за полёт).
        if (pass.CollideBarricades.TryGetValue(entity.Owner, out var cached))
            return cached;

        var hitChance = CalculateHitChance(entity, projComp.Shooter);
        var isPass = !_random.Prob(hitChance);

        pass.CollideBarricades[entity.Owner] = isPass;
        Dirty(projUid, pass);

        return isPass;
    }

    /// <summary>
    /// Линейно интерполирует шанс перехвата между <see cref="BarricadeComponent.MinHitChance"/> и
    /// <see cref="BarricadeComponent.MaxHitChance"/> по дистанции от баррикады до стрелка. Если
    /// стрелок неизвестен/удалён — считаем дистанцию максимальной (худший для прохода случай).
    /// </summary>
    private float CalculateHitChance(Entity<BarricadeComponent> entity, EntityUid? shooter)
    {
        var comp = entity.Comp;

        float distance;
        if (shooter is { } shooterUid && Exists(shooterUid))
        {
            var diff = _transform.GetWorldPosition(entity.Owner) - _transform.GetWorldPosition(shooterUid);
            distance = diff.Length();
        }
        else
        {
            distance = comp.MaxDistance;
        }

        var distanceDiff = comp.MaxDistance - comp.MinDistance;
        if (distanceDiff <= 0f)
            return Math.Clamp(comp.MaxHitChance, 0f, 1f);

        var chanceDiff = comp.MaxHitChance - comp.MinHitChance;
        var increase = Math.Clamp(distance - comp.MinDistance, 0f, distanceDiff) / distanceDiff * chanceDiff;

        return Math.Clamp(comp.MinHitChance + increase, comp.MinHitChance, comp.MaxHitChance);
    }
}
