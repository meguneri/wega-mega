using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Истинно, когда у NPC накоплено урона не меньше <see cref="DamageFraction"/> от порога крита —
/// то есть он сильно ранен. Используется, чтобы включать ветку бегства в HTN скавов.
/// </summary>
public sealed partial class LowHealthPrecondition : HTNPrecondition
{
    [Dependency] private IEntityManager _entManager = default!;

    private MobThresholdSystem _thresholds = default!;
    private DamageableSystem _damageable = default!;

    /// <summary>
    /// Доля от порога крита (по урону), при достижении которой считаем NPC «раненым».
    /// 0.5 — бежит, набрав половину урона до крита; 0.8 — только при тяжёлых ранах.
    /// </summary>
    [DataField]
    public float DamageFraction = 0.5f;

    public override void Initialize(IEntitySystemManager sysManager)
    {
        base.Initialize(sysManager);
        _thresholds = sysManager.GetEntitySystem<MobThresholdSystem>();
        _damageable = sysManager.GetEntitySystem<DamageableSystem>();
    }

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, _entManager))
            return false;

        if (!_thresholds.TryGetIncapThreshold(owner, out var threshold))
            return false;

        return _damageable.GetTotalDamage(owner) >= threshold.Value * DamageFraction;
    }
}
