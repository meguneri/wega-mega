using Content.Shared._Wega.Raid.Components;
using Content.Server.NPC.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server._Wega.Raid.Systems;

/// <summary>
/// Применяет «меткость по типу» (<see cref="RaidSkavAccuracyComponent"/>): при появлении у скава
/// компонента NPC-дальнего боя переустанавливает его конус прицеливания / задержку / скорость
/// доворота из типа. Операнд боя задаёт только цель, эти поля не трогает, поэтому значения держатся.
/// </summary>
public sealed partial class RaidSkavAccuracySystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RaidSkavAccuracyComponent>();
        while (query.MoveNext(out var uid, out var acc))
        {
            if (acc.Applied || !TryComp<NPCRangedCombatComponent>(uid, out var ranged))
                continue;

            ranged.AccuracyThreshold = Angle.FromDegrees(acc.AccuracyDegrees);

            if (acc.ShootDelay is { } delay)
                ranged.ShootDelay = delay;

            if (acc.RotationSpeedDegrees is { } rot)
                ranged.RotationSpeed = Angle.FromDegrees(rot);

            acc.Applied = true;
        }
    }
}
