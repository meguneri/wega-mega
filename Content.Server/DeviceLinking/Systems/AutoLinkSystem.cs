using Content.Server.DeviceLinking.Components;

namespace Content.Server.DeviceLinking.Systems;

/// <summary>
/// Automatically links device-link transmitters and receivers that share the same
/// AutoLink channel when a map is initialized.
///
/// Uses <see cref="EntityManager.AllEntityQueryEnumerator{T}"/> so that entities on
/// a paused (frozen) map are not skipped — regular EntityQueryEnumerator filters out
/// paused entities, which caused AutoLink to silently fail for pre-placed map entities.
/// </summary>
public sealed partial class AutoLinkSystem : EntitySystem
{
    [Dependency] private DeviceLinkSystem _deviceLinkSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AutoLinkTransmitterComponent, MapInitEvent>(OnTransmitterMapInit);
        SubscribeLocalEvent<AutoLinkReceiverComponent, MapInitEvent>(OnReceiverMapInit);
    }

    private void OnTransmitterMapInit(EntityUid uid, AutoLinkTransmitterComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);

        var query = AllEntityQuery<AutoLinkReceiverComponent>();
        while (query.MoveNext(out var receiverUid, out var receiver))
        {
            if (!ReceiverMatchesChannel(receiver, component.AutoLinkChannel))
                continue;

            if (Transform(receiverUid).GridUid != xform.GridUid)
                continue;

            _deviceLinkSystem.LinkDefaults(null, uid, receiverUid, skipRangeCheck: true);
        }
    }

    private void OnReceiverMapInit(EntityUid uid, AutoLinkReceiverComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);

        var query = AllEntityQuery<AutoLinkTransmitterComponent>();
        while (query.MoveNext(out var transmitterUid, out var transmitter))
        {
            if (!ReceiverMatchesChannel(component, transmitter.AutoLinkChannel))
                continue;

            if (Transform(transmitterUid).GridUid != xform.GridUid)
                continue;

            _deviceLinkSystem.LinkDefaults(null, transmitterUid, uid, skipRangeCheck: true);
        }
    }

    private static bool ReceiverMatchesChannel(AutoLinkReceiverComponent receiver, string channel)
    {
        if (receiver.AutoLinkChannel == channel)
            return true;
        return receiver.AutoLinkChannels.Contains(channel);
    }
}
