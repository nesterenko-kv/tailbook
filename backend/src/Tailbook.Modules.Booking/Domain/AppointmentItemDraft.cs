namespace Tailbook.Modules.Booking.Domain;

public sealed record AppointmentItemDraft(
    string ItemType,
    Guid OfferId,
    Guid OfferVersionId,
    string OfferCodeSnapshot,
    string OfferDisplayNameSnapshot,
    int Quantity,
    Guid PriceSnapshotId,
    Guid DurationSnapshotId);
