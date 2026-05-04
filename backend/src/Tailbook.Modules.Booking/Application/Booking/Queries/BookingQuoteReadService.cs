using ErrorOr;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed class BookingQuoteReadService(IBookingSnapshotComposer bookingSnapshotComposer)
{
    public async Task<ErrorOr<QuotePreviewView>> PreviewQuoteAsync(PreviewQuoteQuery command, string? actorUserId, CancellationToken cancellationToken)
    {
        return await bookingSnapshotComposer.CreatePreviewAsync(command, actorUserId, cancellationToken);
    }
}

public sealed record PreviewQuoteQuery(Guid PetId, Guid? GroomerId, IReadOnlyCollection<PreviewQuoteItemQuery> Items);
public sealed record PreviewQuoteItemQuery(Guid OfferId, string? ItemType);
public sealed record QuotePreviewView(PriceSnapshotView PriceSnapshot, DurationSnapshotView DurationSnapshot, IReadOnlyCollection<QuotePreviewItemView> Items);
public sealed record QuotePreviewItemView(Guid OfferId, Guid OfferVersionId, string OfferCode, string OfferType, string DisplayName, decimal PriceAmount, int ServiceMinutes, int ReservedMinutes);
public sealed record PriceSnapshotView(Guid Id, string Currency, decimal TotalAmount, IReadOnlyCollection<PriceSnapshotLineView> Lines);
public sealed record PriceSnapshotLineView(string LineType, string Label, decimal Amount, Guid? SourceRuleId, int SequenceNo);
public sealed record DurationSnapshotView(Guid Id, int ServiceMinutes, int ReservedMinutes, IReadOnlyCollection<DurationSnapshotLineView> Lines);
public sealed record DurationSnapshotLineView(string LineType, string Label, int Minutes, Guid? SourceRuleId, int SequenceNo);
