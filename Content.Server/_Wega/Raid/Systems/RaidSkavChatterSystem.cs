using Content.Server._Wega.Raid.Components;
using Content.Shared._Wega.Raid.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Raid.Systems;

/// <summary>
/// Заставляет скавов (<see cref="RaidSkavChatterComponent"/>) изредка выкрикивать случайные реплики —
/// перекличка между собой, делает локацию живее. Говорят только живые; интервал и шанс случайны,
/// чтобы фразы не сыпались в такт.
/// </summary>
public sealed partial class RaidSkavChatterSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RaidSkavChatterComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RaidSkavChatterComponent comp, MapInitEvent args)
    {
        Reschedule(comp);
    }

    private void Reschedule(RaidSkavChatterComponent comp)
    {
        comp.NextSpeak = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(comp.MinDelay, comp.MaxDelay));
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RaidSkavChatterComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.NextSpeak)
                continue;

            Reschedule(comp);

            if (comp.Phrases.Count == 0 || !_mobState.IsAlive(uid))
                continue;

            if (!_random.Prob(comp.Chance))
                continue;

            var phrase = Loc.GetString(_random.Pick(comp.Phrases));
            _chat.TrySendInGameICMessage(uid, phrase, InGameICChatType.Speak, false);
        }
    }
}
