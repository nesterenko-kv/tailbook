using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Commands;

public sealed record CheckInAppointmentUseCaseCommand(
    Guid AppointmentId,
    Guid ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;
