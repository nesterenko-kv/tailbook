namespace Tailbook.Modules.Booking.Api.Client;

public sealed class ClientBookingRequestItemPayload
{
    public Guid OfferId { get; set; }
    public string? ItemType { get; set; }
    public string? RequestedNotes { get; set; }
}