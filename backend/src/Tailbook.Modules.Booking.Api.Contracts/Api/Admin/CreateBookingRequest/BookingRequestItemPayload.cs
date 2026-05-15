namespace Tailbook.Modules.Booking.Api.Admin.CreateBookingRequest;

public sealed class BookingRequestItemPayload
{
    public Guid OfferId { get; set; }
    public string? ItemType { get; set; }
    public string? RequestedNotes { get; set; }
}