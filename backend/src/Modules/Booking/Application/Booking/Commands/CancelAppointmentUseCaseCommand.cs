using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CancelAppointmentUseCaseCommand(
    Guid AppointmentId,
    int ExpectedVersionNo,
    string ReasonCode,
    string? Notes,
    Guid ActorUserId) : ICommand<ErrorOr<AppointmentDetailView>>;
