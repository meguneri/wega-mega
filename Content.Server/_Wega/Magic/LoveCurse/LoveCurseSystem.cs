using System.Numerics;
using Content.Shared._Wega.Magic.LoveCurse;
using Content.Shared.Gibbing;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Magic.LoveCurse;

public sealed partial class LoveCurseSystem : EntitySystem
{
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly string[] BodySlots = ["jumpsuit", "outerClothing", "underwearbottom"];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoveCurseSpellEvent>(OnSpellCast);
        SubscribeNetworkEvent<LoveCurseTargetSelectedEvent>(OnTargetSelected);
        SubscribeLocalEvent<LoveCurseComponent, GetVerbsEvent<InteractionVerb>>(AddKissVerb);
        SubscribeLocalEvent<LoveCurseComponent, LoveCurseKissDoAfterEvent>(OnKissDoAfter);
        SubscribeLocalEvent<LoveCurseComponent, ExaminedEvent>(OnExamine);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        _expired.Clear();
        var query = EntityQueryEnumerator<LoveCurseComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.ExpiresAt)
                continue;

            _expired.Add(uid);
        }

        foreach (var uid in _expired)
        {
            RemComp<LoveCurseComponent>(uid);
            _gibbing.Gib(uid);
        }
    }

    private readonly List<EntityUid> _expired = new();

    private void OnSpellCast(LoveCurseSpellEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var caster = args.Performer;

        if (!TryComp<ActorComponent>(caster, out var actor))
            return;

        RaiseNetworkEvent(new LoveCurseMenuOpenedEvent(GetNetEntity(caster)), actor.PlayerSession);
    }

    private void OnTargetSelected(LoveCurseTargetSelectedEvent args, EntitySessionEventArgs session)
    {
        var caster = GetEntity(args.Caster);
        var target = GetEntity(args.Target);

        if (!Exists(caster) || !Exists(target) || caster == target)
            return;

        var curse = EnsureComp<LoveCurseComponent>(target);
        curse.ExpiresAt = _timing.CurTime + TimeSpan.FromMinutes(30);
        curse.Caster = caster;
        Dirty(target, curse);

        _popup.PopupEntity(
            Loc.GetString("love-curse-cast-target", ("caster", Identity.Name(caster, EntityManager))),
            target, target, PopupType.LargeCaution);

        _popup.PopupEntity(
            Loc.GetString("love-curse-cast-caster", ("target", Identity.Name(target, EntityManager))),
            caster, caster, PopupType.Medium);
    }

    private void AddKissVerb(Entity<LoveCurseComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (args.User == ent.Owner)
            return;

        var user = args.User;
        var target = ent.Owner;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("love-curse-kiss-verb"),
            Priority = 2,
            Act = () =>
            {
                if (!IsNaked(user))
                {
                    _popup.PopupEntity(Loc.GetString("love-curse-kiss-fail-user"), user, user, PopupType.SmallCaution);
                    return;
                }

                if (!IsNaked(target))
                {
                    _popup.PopupEntity(Loc.GetString("love-curse-kiss-fail-target"), user, user, PopupType.SmallCaution);
                    return;
                }

                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, 3f, new LoveCurseKissDoAfterEvent(), target, target: target)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = false,
                });

                _popup.PopupEntity(
                    Loc.GetString("love-curse-kiss-start", ("user", Identity.Name(user, EntityManager))),
                    target);
            }
        });
    }

    private void OnKissDoAfter(Entity<LoveCurseComponent> ent, ref LoveCurseKissDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var user = args.User;
        var target = ent.Owner;

        if (!IsNaked(user) || !IsNaked(target))
        {
            _popup.PopupEntity(Loc.GetString("love-curse-kiss-fail-clothes"), user, user, PopupType.SmallCaution);
            return;
        }

        _popup.PopupEntity(
            Loc.GetString("love-curse-kiss-success",
                ("user", Identity.Name(user, EntityManager)),
                ("target", Identity.Name(target, EntityManager))),
            target);

        SpawnHearts(target);
        RemComp<LoveCurseComponent>(target);
    }

    private void OnExamine(Entity<LoveCurseComponent> ent, ref ExaminedEvent args)
    {
        var remaining = ent.Comp.ExpiresAt - _timing.CurTime;
        if (remaining <= TimeSpan.Zero)
            return;

        var minutes = (int) remaining.TotalMinutes;
        var seconds = remaining.Seconds;

        args.PushMarkup(Loc.GetString("love-curse-examine",
            ("minutes", minutes),
            ("seconds", seconds)));
    }

    private static readonly SoundSpecifier LickSound =
        new SoundCollectionSpecifier("Licks", AudioParams.Default.WithVolume(4f));

    private void SpawnHearts(EntityUid target)
    {
        _audio.PlayPvs(LickSound, target);

        var coords = _transform.GetMapCoordinates(target);
        for (var i = 0; i < 8; i++)
        {
            var offset = new Vector2(
                _random.NextFloat(-0.6f, 0.6f),
                _random.NextFloat(-0.6f, 0.6f));
            Spawn("EffectHearts", new MapCoordinates(coords.Position + offset, coords.MapId));
        }
    }

    private bool IsNaked(EntityUid uid)
    {
        foreach (var slot in BodySlots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out _))
                return false;
        }
        return true;
    }
}
