using ErrorOr;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public interface IBookingSnapshotComposer
{
    Task<ErrorOr<QuotePreviewView>> CreatePreviewAsync(PreviewQuoteQuery command, Guid? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<AppointmentCompositionResult>> ComposeAppointmentAsync(
        Guid petId,
        Guid groomerId,
        DateTimeOffset startAt,
        IReadOnlyCollection<PreviewQuoteItemQuery> items,
        Guid? actorUserId,
        CancellationToken cancellationToken);
}
