using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Commands;

public sealed record RecordOwnPerformedProcedureUseCaseCommand(
    Guid CurrentUserId,
    Guid VisitId,
    Guid VisitExecutionItemId,
    Guid ProcedureId,
    string? Note) : ICommand<ErrorOr<GroomerVisitDetailView>>;