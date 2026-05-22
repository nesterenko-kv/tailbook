using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Commands;

public sealed record RecordPerformedProcedureUseCaseCommand(
    Guid VisitId,
    Guid VisitExecutionItemId,
    Guid ProcedureId,
    string? Note,
    Guid ActorUserId) : ICommand<ErrorOr<VisitDetailView>>;
