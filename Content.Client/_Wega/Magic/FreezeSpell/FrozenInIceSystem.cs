using Content.Shared._Wega.Magic.FreezeSpell;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Wega.Magic.FreezeSpell;

public sealed partial class FrozenInIceSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private const string RsiPath = "Structures/Specific/Anomalies/ice_anom.rsi";
    private const string RsiState = "anom";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FrozenInIceComponent, ComponentStartup>(OnAdd);
        SubscribeLocalEvent<FrozenInIceComponent, ComponentShutdown>(OnRemove);
    }

    private void OnAdd(EntityUid uid, FrozenInIceComponent comp, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out _))
            return;

        if (_sprite.LayerMapTryGet(uid, FrozenLayerKey.IceOverlay, out _, false))
            return;

        var layerData = new PrototypeLayerData
        {
            Shader = "unshaded",
            RsiPath = RsiPath,
            State = RsiState,
            Color = new Robust.Shared.Maths.Color(0.6f, 0.9f, 1f, 0.75f),
        };

        var layer = _sprite.AddLayer(uid, layerData, null);
        _sprite.LayerMapSet(uid, FrozenLayerKey.IceOverlay, layer);
    }

    private void OnRemove(EntityUid uid, FrozenInIceComponent comp, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out _))
            return;

        if (_sprite.LayerMapTryGet(uid, FrozenLayerKey.IceOverlay, out var layer, false))
            _sprite.RemoveLayer(uid, layer);
    }
}

public enum FrozenLayerKey
{
    IceOverlay,
}
