using ErrorOr;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public interface IGroomerBookingReadService
{
    Task<ErrorOr<PagedResult<GroomerAppointmentListItemView>>> ListAssignedAppointmentsAsync(Guid currentUserId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken);
    Task<ErrorOr<GroomerAppointmentDetailView>> GetAssignedAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken);
}
