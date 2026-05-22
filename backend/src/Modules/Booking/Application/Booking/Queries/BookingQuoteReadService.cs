using ErrorOr;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public sealed class BookingQuoteReadService(IBookingSnapshotComposer bookingSnapshotComposer)
{
    public async Task<ErrorOr<QuotePreviewView>> PreviewQuoteAsync(PreviewQuoteQuery command, Guid? actorUserId, CancellationToken cancellationToken)
    {
        return await bookingSnapshotComposer.CreatePreviewAsync(command, actorUserId, cancellationToken);
    }
}
