using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record ConvertBookingRequestToAppointmentUseCaseCommand(
    Guid BookingRequestId,
    Guid GroomerId,
    DateTimeOffset StartAt,
    Guid ActorUserId) : ICommand<ErrorOr<AppointmentDetailView>>;
