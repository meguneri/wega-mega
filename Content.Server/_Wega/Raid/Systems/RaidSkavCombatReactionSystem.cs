using Content.Server._Wega.Raid.Components;
using Content.Shared._Wega.Raid.Components;
using Content.Server.Chat.Systems;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Raid.Systems;

/// <summary>
/// Боевая реакция скавов: когда скав в бою (есть активная цель у NPC-боя), он изредка выкрикивает
/// боевую реплику и «зовёт» соседних скавов — добавляет им цель в память агра (NPCRetaliation), чтобы
/// стая запоминала и преследовала её. Выкрик и зов троттлятся кулдауном, мёртвые молчат.
/// </summary>
public sealed partial class RaidSkavCombatReactionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private NPCRetaliationSystem _retaliation = default!;

    /// <summary>Радиус, в котором боевой клич поднимает соседних скавов (тайлы).</summary>
    private const float AlertRadius = 12f;

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RaidSkavChatterComponent>();
        while (query.MoveNext(out var uid, out var chatter))
        {
            if (now < chatter.NextCombatBark)
                continue;

            if (!_mobState.IsAlive(uid) || !TryGetCombatTarget(uid, out var target))
                continue;

            chatter.NextCombatBark = now + TimeSpan.FromSeconds(chatter.CombatBarkCooldown);

            if (chatter.CombatPhrases.Count > 0)
                _chat.TrySendInGameICMessage(uid, Loc.GetString(_random.Pick(chatter.CombatPhrases)), InGameICChatType.Speak, false);

            AlertNearby(uid, target);
        }
    }

    /// <summary>Текущая цель NPC-боя скава (дальнего или ближнего), если он в бою.</summary>
    private bool TryGetCombatTarget(EntityUid uid, out EntityUid target)
    {
        if (TryComp<NPCRangedCombatComponent>(uid, out var ranged) && Exists(ranged.Target))
        {
            target = ranged.Target;
            return true;
        }
        if (TryComp<NPCMeleeCombatComponent>(uid, out var melee) && Exists(melee.Target))
        {
            target = melee.Target;
            return true;
        }
        target = default;
        return false;
    }

    /// <summary>Поднимает соседних скавов: добавляет цель в их память агра, чтобы они её преследовали.</summary>
    private void AlertNearby(EntityUid caller, EntityUid target)
    {
        foreach (var (allyUid, _) in _lookup.GetEntitiesInRange<RaidSkavChatterComponent>(Transform(caller).Coordinates, AlertRadius))
        {
            if (allyUid == caller || !_mobState.IsAlive(allyUid))
                continue;

            if (TryComp<NPCRetaliationComponent>(allyUid, out var ret))
                _retaliation.TryRetaliate((allyUid, ret), target);
        }
    }
}
