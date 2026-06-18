using Content.Shared.Audio;
using Content.Shared.Storage.Components;

namespace Content.Shared._Wega.Audio;

/// <summary>
/// Зацикленный <see cref="AmbientSoundComponent"/> на хранилище-ящике играет, только пока ящик
/// открыт: при открытии звук включается, при закрытии — глушится. Достаточно повесить на сущность
/// <see cref="AmbientSoundComponent"/> (с нужным звуком и <c>enabled: false</c>).
/// </summary>
public sealed partial class StorageAmbientSoundSystem : EntitySystem
{
    [Dependency] private SharedAmbientSoundSystem _ambient = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AmbientSoundComponent, StorageAfterOpenEvent>(OnOpen);
        SubscribeLocalEvent<AmbientSoundComponent, StorageAfterCloseEvent>(OnClose);
    }

    private void OnOpen(Entity<AmbientSoundComponent> ent, ref StorageAfterOpenEvent args)
    {
        _ambient.SetAmbience(ent, true, ent.Comp);
    }

    private void OnClose(Entity<AmbientSoundComponent> ent, ref StorageAfterCloseEvent args)
    {
        _ambient.SetAmbience(ent, false, ent.Comp);
    }
}
