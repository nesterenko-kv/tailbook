namespace Tailbook.Modules.Booking.Api.Client;

public sealed class PreviewMyQuoteResponse
{
    public string Currency { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ServiceMinutes { get; set; }
    public int ReservedMinutes { get; set; }
    public QuoteItemPayload[] Items { get; set; } = [];
    public PriceLinePayload[] PriceLines { get; set; } = [];
    public DurationLinePayload[] DurationLines { get; set; } = [];

    public sealed class QuoteItemPayload
    {
        public Guid OfferId { get; set; }
        public string OfferType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal PriceAmount { get; set; }
        public int ServiceMinutes { get; set; }
        public int ReservedMinutes { get; set; }
    }

    public sealed class PriceLinePayload
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public sealed class DurationLinePayload
    {
        public string Label { get; set; } = string.Empty;
        public int Minutes { get; set; }
    }
}