using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

public sealed class RecordOwnPerformedProcedureUseCaseCommandHandler(
    IGroomerVisitReadService groomerVisitReadService,
    RecordPerformedProcedureUseCaseCommandHandler recordPerformedProcedureHandler)
    : ICommandHandler<RecordOwnPerformedProcedureUseCaseCommand, ErrorOr<GroomerVisitDetailView>>
{
    public async Task<ErrorOr<GroomerVisitDetailView>> ExecuteAsync(RecordOwnPerformedProcedureUseCaseCommand command, CancellationToken ct = default)
    {
        var existing = await groomerVisitReadService.GetVisitAsync(command.CurrentUserId, command.VisitId, ct);
        if (existing.IsError)
        {
            return existing.Errors;
        }

        var result = await recordPerformedProcedureHandler.ExecuteAsync(
            new RecordPerformedProcedureUseCaseCommand(
                command.VisitId,
                command.VisitExecutionItemId,
                command.ProcedureId,
                command.Note,
                command.CurrentUserId),
            ct);

        return result.IsError ? result.Errors : GroomerVisitMapper.Map(result.Value);
    }
}
