using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

public sealed class RecordOwnSkippedComponentUseCaseCommandHandler(
    IGroomerVisitReadService groomerVisitReadService,
    RecordSkippedComponentUseCaseCommandHandler recordSkippedComponentHandler)
    : ICommandHandler<RecordOwnSkippedComponentUseCaseCommand, ErrorOr<GroomerVisitDetailView>>
{
    public async Task<ErrorOr<GroomerVisitDetailView>> ExecuteAsync(RecordOwnSkippedComponentUseCaseCommand command, CancellationToken ct = default)
    {
        var existing = await groomerVisitReadService.GetVisitAsync(command.CurrentUserId, command.VisitId, ct);
        if (existing.IsError)
        {
            return existing.Errors;
        }

        var result = await recordSkippedComponentHandler.ExecuteAsync(
            new RecordSkippedComponentUseCaseCommand(
                command.VisitId,
                command.VisitExecutionItemId,
                command.OfferVersionComponentId,
                command.OmissionReasonCode,
                command.Note,
                command.CurrentUserId),
            ct);

        return result.IsError ? result.Errors : GroomerVisitMapper.Map(result.Value);
    }
}
