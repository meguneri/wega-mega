using Content.Server.Body;
using Content.Server.Humanoid.Systems;
using Content.Shared.Body;
using Content.Shared.Height;

namespace Content.Server._Wega.Humanoid;

/// <summary>
/// Applies <see cref="FixedHumanoidAppearanceComponent"/> after randomization
/// (<see cref="RandomHumanoidAppearanceSystem"/>) and the visual body init
/// (<see cref="VisualBodySystem"/>) have run, so the forced look always wins.
/// </summary>
public sealed partial class FixedHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private HeightSystem _height = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FixedHumanoidAppearanceComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(RandomHumanoidAppearanceSystem), typeof(VisualBodySystem) });
    }

    private void OnMapInit(Entity<FixedHumanoidAppearanceComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Markings.Count > 0)
            _visualBody.ApplyMarkings(ent.Owner, ent.Comp.Markings);

        if (ent.Comp.Height is { } height)
            _height.SetHeight(ent.Owner, height);
    }
}
