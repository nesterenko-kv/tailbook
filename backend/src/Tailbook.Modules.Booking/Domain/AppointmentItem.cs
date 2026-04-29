namespace Tailbook.Modules.Booking.Domain;

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
    public DateTime CreatedAtUtc { get; private set; }

    internal static AppointmentItem Create(
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
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment item id is required.");
        }

        if (appointmentId == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment item must belong to an appointment.");
        }

        if (string.IsNullOrWhiteSpace(itemType))
        {
            throw new InvalidOperationException("Appointment item type is required.");
        }

        if (offerId == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment item must reference an offer.");
        }

        if (offerVersionId == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment item must reference an offer version.");
        }

        if (string.IsNullOrWhiteSpace(offerCodeSnapshot))
        {
            throw new InvalidOperationException("Appointment item must include an offer code snapshot.");
        }

        if (string.IsNullOrWhiteSpace(offerDisplayNameSnapshot))
        {
            throw new InvalidOperationException("Appointment item must include an offer display name snapshot.");
        }

        if (quantity <= 0)
        {
            throw new InvalidOperationException("Appointment item quantity must be greater than zero.");
        }

        if (priceSnapshotId == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment item must reference a price snapshot.");
        }

        if (durationSnapshotId == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment item must reference a duration snapshot.");
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
            CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc)
        };
    }
}
