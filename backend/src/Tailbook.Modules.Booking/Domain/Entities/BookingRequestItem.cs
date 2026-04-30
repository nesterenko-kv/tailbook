namespace Tailbook.Modules.Booking.Domain.Entities;

public sealed class BookingRequestItem
{
    public Guid Id { get; set; }
    public Guid BookingRequestId { get; set; }
    public Guid OfferId { get; set; }
    public Guid? OfferVersionId { get; set; }
    public string? ItemType { get; set; }
    public string? RequestedNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
