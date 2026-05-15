using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Commands;

public sealed record CloseVisitUseCaseCommand(
    Guid VisitId,
    Guid ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;
