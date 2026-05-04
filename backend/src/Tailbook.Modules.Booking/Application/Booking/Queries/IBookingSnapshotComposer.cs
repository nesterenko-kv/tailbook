using ErrorOr;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public interface IBookingSnapshotComposer
{
    Task<ErrorOr<QuotePreviewView>> CreatePreviewAsync(PreviewQuoteQuery command, string? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<AppointmentCompositionResult>> ComposeAppointmentAsync(
        Guid petId,
        Guid groomerId,
        DateTime startAtUtc,
        IReadOnlyCollection<PreviewQuoteItemQuery> items,
        string? actorUserId,
        CancellationToken cancellationToken);
}
