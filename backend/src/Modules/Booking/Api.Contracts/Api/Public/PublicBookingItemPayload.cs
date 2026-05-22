namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicBookingItemPayload
{
    public Guid OfferId { get; set; }
    public string? ItemType { get; set; }
    public string? RequestedNotes { get; set; }
}