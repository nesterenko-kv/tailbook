using ErrorOr;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public interface IBookingSnapshotComposer
{
    Task<ErrorOr<QuotePreviewView>> CreatePreviewAsync(PreviewQuoteCommand command, string? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<AppointmentCompositionResult>> ComposeAppointmentAsync(
        Guid petId,
        Guid groomerId,
        DateTime startAtUtc,
        IReadOnlyCollection<PreviewQuoteItemCommand> items,
        string? actorUserId,
        CancellationToken cancellationToken);
}
