using Content.Shared.Item;

namespace Content.Shared._Wega.CannotPickup;

/// <summary>
/// Cancels every pickup attempt by an entity with <see cref="CannotPickupComponent"/>.
/// </summary>
public sealed partial class CannotPickupSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CannotPickupComponent, PickupAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(EntityUid uid, CannotPickupComponent component, PickupAttemptEvent args)
    {
        args.Cancel();
    }
}
