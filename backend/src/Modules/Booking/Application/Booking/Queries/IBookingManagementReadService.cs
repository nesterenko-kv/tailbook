namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public interface IBookingManagementReadService
{
    Task<PagedResult<BookingRequestListItemView>> ListBookingRequestsAsync(string? search, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<BookingRequestDetailView?> GetBookingRequestAsync(Guid bookingRequestId, CancellationToken cancellationToken);
    Task<Guid?> GetAppointmentIdByBookingRequestAsync(Guid bookingRequestId, CancellationToken cancellationToken);
    Task<PagedResult<AppointmentListItemView>> ListAppointmentsAsync(string? search, DateTimeOffset? from, DateTimeOffset? to, Guid? groomerId, int page, int pageSize, CancellationToken cancellationToken);
    Task<AppointmentDetailView?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken);
}
