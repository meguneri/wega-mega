using Content.Shared.Audio;
using Content.Shared.Hands;

namespace Content.Shared._Wega.Audio;

/// <summary>
/// Toggles <see cref="AmbientSoundComponent"/> on/off as the item enters or
/// leaves a hand, so the passive sound only plays while the item is held.
/// </summary>
public sealed partial class AmbientSoundOnHeldSystem : EntitySystem
{
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmbientSoundOnHeldComponent, GotEquippedHandEvent>(OnEquippedHand);
        SubscribeLocalEvent<AmbientSoundOnHeldComponent, GotUnequippedHandEvent>(OnUnequippedHand);
        SubscribeLocalEvent<AmbientSoundOnHeldComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<AmbientSoundOnHeldComponent> ent, ref MapInitEvent args)
    {
        // Start silent — only hums once picked up.
        if (TryComp<AmbientSoundComponent>(ent, out var ambience))
            _ambient.SetAmbience(ent, false, ambience);
    }

    private void OnEquippedHand(Entity<AmbientSoundOnHeldComponent> ent, ref GotEquippedHandEvent args)
    {
        if (TryComp<AmbientSoundComponent>(ent, out var ambience))
            _ambient.SetAmbience(ent, true, ambience);
    }

    private void OnUnequippedHand(Entity<AmbientSoundOnHeldComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (TryComp<AmbientSoundComponent>(ent, out var ambience))
            _ambient.SetAmbience(ent, false, ambience);
    }
}
