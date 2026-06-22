using Content.Server.Administration.Logs;
using Content.Server._Wega.Duel.Components;
using Content.Server._Wega.Duel.Systems;
using Content.Server.Hands.Systems;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Random;

namespace Content.Server._Wega.Fun;

/// <summary>
/// Бросок «кубика лучника» (<see cref="ArenaBowLuckDieComponent"/>): использование в руке катит d8,
/// показывает выпавшее число и выдаёт в руку соответствующий лук жёсткого света (1-7 — залоченные
/// типы стрел, 8 — полный лук со всеми режимами). Кубик одноразовый — уходит в nullspace и удаляется.
/// </summary>
public sealed partial class ArenaBowLuckDieSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private DuelArenaCleanupSystem _arenaCleanup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArenaBowLuckDieComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, ArenaBowLuckDieComponent comp, UseInHandEvent args)
    {
        if (args.Handled || comp.Outcomes.Count == 0)
            return;

        // Катим число от 1 до граней; индекс в списке исходов = число − 1.
        var roll = _random.Next(1, comp.Outcomes.Count + 1);
        var proto = comp.Outcomes[roll - 1];

        // Лук и бонусы помечаем для очистки арены, если кубик был выдан ареной ИЛИ пользователь
        // бросил кубик прямо на гриде активной дуэли (кубик куплен из аплинка — метки нет, но
        // спавн происходит в зоне боя, поэтому лук всё равно должен убраться после дуэли).
        var issued = HasComp<ArenaIssuedItemComponent>(uid)
            || _arenaCleanup.IsOnActiveArenaGrid(args.User);

        var coords = Transform(args.User).Coordinates;
        var bow = Spawn(proto, coords);
        if (issued)
            _arenaCleanup.MarkIssuedRecursive(bow);

        // Бонусный дроп под ноги по выпавшему числу (например, наряд на «семёрку»).
        if (comp.BonusDrops.TryGetValue(roll, out var bonus))
        {
            foreach (var bonusProto in bonus)
            {
                var dropped = Spawn(bonusProto, coords);
                if (issued)
                    _arenaCleanup.MarkIssuedRecursive(dropped);
            }
        }

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low,
            $"{ToPrettyString(args.User)} rolled {roll} on {ToPrettyString(uid)} and got {ToPrettyString(bow)}");

        _popup.PopupEntity(Loc.GetString("arena-bow-die-rolled", ("roll", roll)), args.User, args.User, PopupType.Large);
        _audio.PlayPvs(comp.RollSound, coords);

        // Освобождаем руку под лук и убираем кубик (удалять прямо в шине событий нельзя).
        _transform.DetachEntity(uid, Transform(uid));
        QueueDel(uid);

        _hands.PickupOrDrop(args.User, bow);

        args.Handled = true;
    }
}
