using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public interface IClientPortalBookingReadService
{
    Task<IReadOnlyCollection<ClientBookableOfferView>?> ListMyBookableOffersAsync(Guid clientId, Guid petId, CancellationToken cancellationToken);
    Task<ErrorOr<QuotePreviewView>> PreviewMyQuoteAsync(ClientPortalActor actor, PreviewQuoteCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ClientAppointmentSummaryView>> ListMyAppointmentsAsync(Guid clientId, DateTime? fromUtc, CancellationToken cancellationToken);
    Task<ClientAppointmentDetailView?> GetMyAppointmentAsync(Guid clientId, Guid appointmentId, CancellationToken cancellationToken);
}
