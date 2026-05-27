using Content.Server.Storage.EntitySystems;
using Content.Shared._Wega.Storage.Components;
using Robust.Shared.Spawners;

namespace Content.Server._Wega.Storage.Systems;

/// <summary>
/// Ejects all <see cref="Content.Shared.Storage.EntityStorageComponent"/> contents before the
/// entity is removed by <see cref="TimedDespawnComponent"/>, so players or items inside are
/// not deleted along with the container.
/// </summary>
public sealed class EjectStorageOnDespawnSystem : EntitySystem
{
    [Dependency] private readonly EntityStorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EjectStorageOnDespawnComponent, TimedDespawnEvent>(OnDespawn);
    }

    private void OnDespawn(EntityUid uid, EjectStorageOnDespawnComponent comp, ref TimedDespawnEvent args)
    {
        _storage.EmptyContents(uid);
    }
}
