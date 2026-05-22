namespace Tailbook.Modules.Booking.Api.Client;

public sealed class PreviewMyQuoteItemRequest
{
    public Guid OfferId { get; set; }
    public string? ItemType { get; set; }
}