using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record RescheduleAppointmentUseCaseCommand(
    Guid AppointmentId,
    Guid GroomerId,
    DateTimeOffset StartAt,
    int ExpectedVersionNo,
    Guid ActorUserId) : ICommand<ErrorOr<AppointmentDetailView>>;
