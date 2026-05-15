namespace Tailbook.Modules.Booking.Api.Admin.PreviewQuote;

public sealed class PreviewQuoteResponse
{
    public PriceSnapshotPayload PriceSnapshot { get; set; } = new();
    public DurationSnapshotPayload DurationSnapshot { get; set; } = new();
    public QuotePreviewItemPayload[] Items { get; set; } = [];

    public sealed class PriceSnapshotPayload
    {
        public Guid Id { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public PriceSnapshotLinePayload[] Lines { get; set; } = [];
    }

    public sealed class PriceSnapshotLinePayload
    {
        public string LineType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Guid? SourceRuleId { get; set; }
        public int SequenceNo { get; set; }
    }

    public sealed class DurationSnapshotPayload
    {
        public Guid Id { get; set; }
        public int ServiceMinutes { get; set; }
        public int ReservedMinutes { get; set; }
        public DurationSnapshotLinePayload[] Lines { get; set; } = [];
    }

    public sealed class DurationSnapshotLinePayload
    {
        public string LineType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Minutes { get; set; }
        public Guid? SourceRuleId { get; set; }
        public int SequenceNo { get; set; }
    }

    public sealed class QuotePreviewItemPayload
    {
        public Guid OfferId { get; set; }
        public Guid OfferVersionId { get; set; }
        public string OfferCode { get; set; } = string.Empty;
        public string OfferType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal PriceAmount { get; set; }
        public int ServiceMinutes { get; set; }
        public int ReservedMinutes { get; set; }
    }
}