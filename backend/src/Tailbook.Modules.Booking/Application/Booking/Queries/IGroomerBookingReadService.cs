namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public interface IGroomerBookingReadService
{
    Task<PagedResult<GroomerAppointmentListItemView>> ListAssignedAppointmentsAsync(Guid currentUserId, DateTime? fromUtc, DateTime? toUtc, int page, int pageSize, CancellationToken cancellationToken);
    Task<GroomerAppointmentDetailView?> GetAssignedAppointmentAsync(Guid currentUserId, Guid appointmentId, CancellationToken cancellationToken);
}
