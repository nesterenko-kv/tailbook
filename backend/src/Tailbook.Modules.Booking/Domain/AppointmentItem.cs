namespace Tailbook.Modules.Booking.Domain;

public sealed class AppointmentItem
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public Guid OfferId { get; set; }
    public Guid OfferVersionId { get; set; }
    public string OfferCodeSnapshot { get; set; } = string.Empty;
    public string OfferDisplayNameSnapshot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Guid PriceSnapshotId { get; set; }
    public Guid DurationSnapshotId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
