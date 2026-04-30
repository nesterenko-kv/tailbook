namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public interface IBookingManagementReadService
{
    Task<PagedResult<BookingRequestListItemView>> ListBookingRequestsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<BookingRequestDetailView?> GetBookingRequestAsync(Guid bookingRequestId, CancellationToken cancellationToken);
    Task<PagedResult<AppointmentListItemView>> ListAppointmentsAsync(DateTime? fromUtc, DateTime? toUtc, Guid? groomerId, int page, int pageSize, CancellationToken cancellationToken);
    Task<AppointmentDetailView?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken);
}
