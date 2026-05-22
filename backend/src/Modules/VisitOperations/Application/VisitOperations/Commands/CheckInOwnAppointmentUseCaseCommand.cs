using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Commands;

public sealed record CheckInOwnAppointmentUseCaseCommand(
    Guid CurrentUserId,
    Guid AppointmentId) : ICommand<ErrorOr<GroomerVisitDetailView>>;