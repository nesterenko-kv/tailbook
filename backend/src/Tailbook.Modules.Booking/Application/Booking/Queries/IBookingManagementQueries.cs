using ErrorOr;

namespace Tailbook.Modules.Booking.Application.Booking.Queries;

public interface IBookingManagementQueries
{
    Task<ErrorOr<BookingRequestDetailView>> CreateBookingRequestAsync(CreateBookingRequestCommand command, string? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<BookingRequestDetailView>> AttachBookingRequestContextAsync(AttachBookingRequestContextCommand command, string? actorUserId, CancellationToken cancellationToken);
    Task<PagedResult<BookingRequestListItemView>> ListBookingRequestsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<BookingRequestDetailView?> GetBookingRequestAsync(Guid bookingRequestId, CancellationToken cancellationToken);
    Task<ErrorOr<AppointmentDetailView>> ConvertBookingRequestToAppointmentAsync(ConvertBookingRequestToAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<AppointmentDetailView>> CreateAppointmentAsync(CreateAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken);
    Task<PagedResult<AppointmentListItemView>> ListAppointmentsAsync(DateTime? fromUtc, DateTime? toUtc, Guid? groomerId, int page, int pageSize, CancellationToken cancellationToken);
    Task<AppointmentDetailView?> GetAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken);
    Task<ErrorOr<AppointmentDetailView>> RescheduleAppointmentAsync(RescheduleAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken);
    Task<ErrorOr<AppointmentDetailView>> CancelAppointmentAsync(CancelAppointmentCommand command, string? actorUserId, CancellationToken cancellationToken);
}
