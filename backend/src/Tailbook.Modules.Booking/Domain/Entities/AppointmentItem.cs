using ErrorOr;

namespace Tailbook.Modules.Booking.Domain.Entities;

public sealed class AppointmentItem
{
    private AppointmentItem()
    {
    }

    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string ItemType { get; private set; } = string.Empty;
    public Guid OfferId { get; private set; }
    public Guid OfferVersionId { get; private set; }
    public string OfferCodeSnapshot { get; private set; } = string.Empty;
    public string OfferDisplayNameSnapshot { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public Guid PriceSnapshotId { get; private set; }
    public Guid DurationSnapshotId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    internal static ErrorOr<AppointmentItem> Create(
        Guid id,
        Guid appointmentId,
        string itemType,
        Guid offerId,
        Guid offerVersionId,
        string offerCodeSnapshot,
        string offerDisplayNameSnapshot,
        int quantity,
        Guid priceSnapshotId,
        Guid durationSnapshotId,
        DateTimeOffset createdAt)
    {
        List<Error> errors = [];

        if (id == Guid.Empty)
        {
            errors.Add(Error.Validation("Booking.AppointmentItemIdRequired", "Appointment item id is required."));
        }

        if (appointmentId == Guid.Empty)
        {
            errors.Add(Error.Validation("Booking.AppointmentItemAppointmentRequired", "Appointment item must belong to an appointment."));
        }

        if (string.IsNullOrWhiteSpace(itemType))
        {
            errors.Add(Error.Validation("Booking.AppointmentItemTypeRequired", "Appointment item type is required."));
        }

        if (offerId == Guid.Empty)
        {
            errors.Add(Error.Validation("Booking.AppointmentItemOfferRequired", "Appointment item must reference an offer."));
        }

        if (offerVersionId == Guid.Empty)
        {
            errors.Add(Error.Validation("Booking.AppointmentItemOfferVersionRequired", "Appointment item must reference an offer version."));
        }

        if (string.IsNullOrWhiteSpace(offerCodeSnapshot))
        {
            errors.Add(Error.Validation("Booking.AppointmentItemOfferCodeRequired", "Appointment item must include an offer code snapshot."));
        }

        if (string.IsNullOrWhiteSpace(offerDisplayNameSnapshot))
        {
            errors.Add(Error.Validation("Booking.AppointmentItemOfferDisplayNameRequired", "Appointment item must include an offer display name snapshot."));
        }

        if (quantity <= 0)
        {
            errors.Add(Error.Validation("Booking.AppointmentItemQuantityInvalid", "Appointment item quantity must be greater than zero."));
        }

        if (priceSnapshotId == Guid.Empty)
        {
            errors.Add(Error.Validation("Booking.AppointmentItemPriceSnapshotRequired", "Appointment item must reference a price snapshot."));
        }

        if (durationSnapshotId == Guid.Empty)
        {
            errors.Add(Error.Validation("Booking.AppointmentItemDurationSnapshotRequired", "Appointment item must reference a duration snapshot."));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new AppointmentItem
        {
            Id = id,
            AppointmentId = appointmentId,
            ItemType = itemType.Trim(),
            OfferId = offerId,
            OfferVersionId = offerVersionId,
            OfferCodeSnapshot = offerCodeSnapshot.Trim(),
            OfferDisplayNameSnapshot = offerDisplayNameSnapshot.Trim(),
            Quantity = quantity,
            PriceSnapshotId = priceSnapshotId,
            DurationSnapshotId = durationSnapshotId,
            CreatedAt = createdAt.ToUniversalTime()
        };
    }
}
