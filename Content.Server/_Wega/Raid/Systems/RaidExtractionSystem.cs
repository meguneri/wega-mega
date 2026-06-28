using System.Linq;
using Content.Server._Wega.Raid.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server._Wega.Raid.Systems;

/// <summary>
/// Обслуживает точки экстракта (<see cref="RaidExtractionPointComponent"/>). Каждый тик копит прогресс
/// эвакуации живым рейдерам, стоящим в зоне; достоял <see cref="RaidExtractionPointComponent.ExtractTime"/>
/// секунд — вызывает <see cref="RaidControllerSystem.ExtractRaider"/>. Прогресс сбрасывается, как только
/// рейдер покинул зону или упал в крит.
/// </summary>
public sealed partial class RaidExtractionSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private RaidControllerSystem _raid = default!;

    public override void Update(float frameTime)
    {
        if (!_raid.TryGetController(out var ctrlUid, out var ctrl) || !ctrl.Active)
            return;

        var query = EntityQueryEnumerator<RaidExtractionPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var point, out var xform))
        {
            UpdatePoint(ctrlUid, ctrl, uid, point, xform, frameTime);
        }
    }

    private void UpdatePoint(
        EntityUid controller,
        RaidControllerComponent ctrl,
        EntityUid uid,
        RaidExtractionPointComponent point,
        TransformComponent xform,
        float frameTime)
    {
        var coords = xform.Coordinates;

        // Живые рейдеры в зоне на этом тике.
        var inZone = new HashSet<EntityUid>();
        foreach (var (mob, _) in _lookup.GetEntitiesInRange<MobStateComponent>(coords, point.Range))
        {
            if (!ctrl.Raiders.Contains(mob) || !_mobState.IsAlive(mob))
                continue;

            inZone.Add(mob);

            // Новый в зоне — играем звук начала эвакуации.
            if (!point.Progress.ContainsKey(mob))
            {
                point.Progress[mob] = 0f;
                if (point.StartSound != null)
                    _audio.PlayEntity(point.StartSound, mob, uid);
            }
        }

        // Сброс прогресса у тех, кто вышел из зоны (или умер).
        foreach (var mob in point.Progress.Keys.ToList())
        {
            if (!inZone.Contains(mob))
                point.Progress.Remove(mob);
        }

        // Копим прогресс и эвакуируем достоявших.
        foreach (var mob in inZone)
        {
            var before = point.Progress[mob];
            var after = before + frameTime;
            point.Progress[mob] = after;

            if (after >= point.ExtractTime)
            {
                point.Progress.Remove(mob);
                if (point.ExtractSound != null)
                    _audio.PlayEntity(point.ExtractSound, mob, uid);
                _raid.ExtractRaider(controller, ctrl, mob);
                continue;
            }

            // Раз в секунду показываем процент эвакуации.
            if ((int)before != (int)after)
            {
                var percent = (int)(after / point.ExtractTime * 100f);
                _popup.PopupEntity(Loc.GetString("raid-extracting", ("percent", percent)), mob, mob);
            }
        }
    }
}
