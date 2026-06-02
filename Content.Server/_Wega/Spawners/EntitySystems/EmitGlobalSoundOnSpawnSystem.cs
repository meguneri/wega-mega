using System.Collections.Generic;
using Content.Server._Wega.Spawners.Components;
using Content.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Wega.Spawners.EntitySystems;

/// <summary>
/// Handles EmitGlobalSoundOnSpawnComponent — plays a station-wide sound
/// when the entity is initialized on the map.
///
/// Sound is deferred to the next game Update() tick rather than fired directly
/// inside MapInitEvent. This avoids raising network events during entity
/// initialization (which can cause crashes or client desync).
/// </summary>
public sealed partial class EmitGlobalSoundOnSpawnSystem : EntitySystem
{
    [Dependency] private ServerGlobalSoundSystem _globalSound = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    // Pending sounds to play on the next Update() tick.
    private readonly Queue<(EntityUid Uid, ResolvedSoundSpecifier Sound, AudioParams Params)> _pending = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmitGlobalSoundOnSpawnComponent, MapInitEvent>(OnSpawn);
    }

    private void OnSpawn(EntityUid uid, EmitGlobalSoundOnSpawnComponent component, MapInitEvent args)
    {
        // Resolve the sound now (while the component is still valid),
        // but defer the actual network event to the next tick.
        _pending.Enqueue((uid, _audio.ResolveSound(component.Sound), component.Params));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        while (_pending.TryDequeue(out var entry))
        {
            // Entity might have been deleted between MapInit and this tick — skip if so.
            if (!Exists(entry.Uid))
                continue;

            try
            {
                _globalSound.PlayGlobalOnStation(entry.Uid, entry.Sound, entry.Params);
            }
            catch (Exception e)
            {
                Log.Error($"EmitGlobalSoundOnSpawnSystem: failed to play global sound for {entry.Uid}: {e}");
            }
        }
    }
}
