using Content.Shared.Humanoid;

namespace Content.Shared.Height;

public sealed partial class HeightSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmallHeightComponent, ComponentStartup>(OnSmallHeightComponentStartup);
        SubscribeLocalEvent<BigHeightComponent, ComponentStartup>(OnBigHeightComponentStartup);

        SubscribeLocalEvent<SmallHeightComponent, ComponentShutdown>(OnSmallHeightComponentShutdown);
        SubscribeLocalEvent<BigHeightComponent, ComponentShutdown>(OnBigHeightComponentShutdown);
    }

    /// <summary>
    /// Forces a humanoid's height (in cm) and replicates it to clients.
    /// </summary>
    public void SetHeight(Entity<HumanoidProfileComponent?> ent, float height)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Height = height;
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnSmallHeightComponentStartup(Entity<SmallHeightComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoid))
            return;

        ent.Comp.LastHeight = humanoid.Height;
        humanoid.Height = 140.0f;

        Dirty(ent.Owner, humanoid);
    }

    private void OnBigHeightComponentStartup(Entity<BigHeightComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoid))
            return;

        ent.Comp.LastHeight = humanoid.Height;
        humanoid.Height = humanoid.Height < 240.0f
            ? 240.0f : 300.0f;

        Dirty(ent.Owner, humanoid);
    }

    private void OnSmallHeightComponentShutdown(Entity<SmallHeightComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoid) || ent.Comp.LastHeight == default)
            return;

        humanoid.Height = ent.Comp.LastHeight;

        Dirty(ent.Owner, humanoid);
    }

    private void OnBigHeightComponentShutdown(Entity<BigHeightComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoid) || ent.Comp.LastHeight == default)
            return;

        humanoid.Height = ent.Comp.LastHeight;

        Dirty(ent.Owner, humanoid);
    }
}
