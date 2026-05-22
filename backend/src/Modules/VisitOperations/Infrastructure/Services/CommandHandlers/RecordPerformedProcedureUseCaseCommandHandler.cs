using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

public sealed class RecordPerformedProcedureUseCaseCommandHandler(
    AppDbContext dbContext,
    IVisitReadService visitReadService,
    IAppointmentVisitService appointmentVisitService,
    IVisitCatalogReadService visitCatalogReadService,
    TimeProvider timeProvider)
    : ICommandHandler<RecordPerformedProcedureUseCaseCommand, ErrorOr<VisitDetailView>>
{
    public async Task<ErrorOr<VisitDetailView>> ExecuteAsync(RecordPerformedProcedureUseCaseCommand command, CancellationToken ct = default)
    {
        var visit = await LoadVisitAggregateAsync(command.VisitId, ct);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        var wasOpen = visit.Value.Status == VisitStatusCodes.Open;

        var procedure = await visitCatalogReadService.GetProcedureAsync(command.ProcedureId, ct);
        if (procedure is null)
        {
            return Error.NotFound("VisitOperations.ProcedureNotFound", "Procedure does not exist.");
        }

        var executionItem = visit.Value.ExecutionItems.SingleOrDefault(x => x.Id == command.VisitExecutionItemId);
        if (executionItem is null)
        {
            return Error.NotFound("VisitOperations.ExecutionItemNotFound", "Visit execution item does not exist.");
        }

        if (visit.Value.Status is not VisitStatusCodes.Open and not VisitStatusCodes.InProgress)
        {
            return Error.Conflict("VisitOperations.VisitNotEditable", "Visit is not editable in its current status.");
        }

        if (executionItem.PerformedProcedures.Any(x => x.ProcedureId == procedure.Id))
        {
            return Error.Conflict("VisitOperations.ProcedureAlreadyRecorded", "Procedure has already been recorded for this execution item.");
        }

        var performedProcedure = visit.Value.RecordPerformedProcedure(
            command.VisitExecutionItemId,
            new VisitPerformedProcedureDraft(
                procedure.Id,
                procedure.Code,
                procedure.Name,
                command.Note),
            command.ActorUserId,
            timeProvider.GetUtcNow());
        if (performedProcedure.IsError)
        {
            return performedProcedure.Errors;
        }

        dbContext.Set<VisitPerformedProcedure>().Add(performedProcedure.Value);

        if (wasOpen && visit.Value.Status == VisitStatusCodes.InProgress)
        {
            var markInProgressResult = await appointmentVisitService.MarkInProgressAsync(visit.Value.AppointmentId, command.ActorUserId, ct);
            if (markInProgressResult.IsError)
            {
                return markInProgressResult.Errors;
            }
        }

        await dbContext.SaveChangesAsync(ct);
        return await ReadVisitAsync(command.VisitId, command.ActorUserId, ct);
    }

    private async Task<ErrorOr<Visit>> LoadVisitAggregateAsync(Guid visitId, CancellationToken cancellationToken)
    {
        var visit = await dbContext.Set<Visit>()
            .Include(x => x.ExecutionItems)
            .ThenInclude(x => x.PerformedProcedures)
            .Include(x => x.ExecutionItems)
            .ThenInclude(x => x.SkippedComponents)
            .Include(x => x.PriceAdjustments)
            .SingleOrDefaultAsync(x => x.Id == visitId, cancellationToken);

        return visit is null
            ? Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.")
            : visit;
    }

    private async Task<ErrorOr<VisitDetailView>> ReadVisitAsync(Guid visitId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var visit = await visitReadService.GetVisitAsync(visitId, actorUserId, cancellationToken);
        return visit is null
            ? Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.")
            : visit;
    }
}
