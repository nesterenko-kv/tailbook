namespace Tailbook.Modules.Booking.Api.Admin.PreviewQuote;

public sealed class PreviewQuoteItemRequest
{
    public string? ItemType { get; set; }
    public Guid OfferId { get; set; }
}