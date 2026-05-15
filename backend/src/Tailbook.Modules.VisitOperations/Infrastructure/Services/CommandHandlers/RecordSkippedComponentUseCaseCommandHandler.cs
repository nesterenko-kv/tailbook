using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

public sealed class RecordSkippedComponentUseCaseCommandHandler(
    AppDbContext dbContext,
    IVisitReadService visitReadService,
    IAppointmentVisitService appointmentVisitService,
    IVisitCatalogReadService visitCatalogReadService,
    TimeProvider timeProvider)
    : ICommandHandler<RecordSkippedComponentUseCaseCommand, ErrorOr<VisitDetailView>>
{
    public async Task<ErrorOr<VisitDetailView>> ExecuteAsync(RecordSkippedComponentUseCaseCommand command, CancellationToken ct = default)
    {
        var visit = await LoadVisitAggregateAsync(command.VisitId, ct);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        var wasOpen = visit.Value.Status == VisitStatusCodes.Open;
        var executionItem = visit.Value.ExecutionItems.SingleOrDefault(x => x.Id == command.VisitExecutionItemId);
        if (executionItem is null)
        {
            return Error.NotFound("VisitOperations.ExecutionItemNotFound", "Visit execution item does not exist.");
        }

        var component = await visitCatalogReadService.GetComponentAsync(command.OfferVersionComponentId, ct);
        if (component is null)
        {
            return Error.NotFound("VisitOperations.ComponentNotFound", "Offer version component does not exist.");
        }

        if (component.OfferVersionId != executionItem.OfferVersionId)
        {
            return Error.Validation("VisitOperations.ComponentExecutionItemMismatch", "Selected component does not belong to this visit execution item.");
        }

        if (!string.Equals(component.ComponentRole, "Included", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("VisitOperations.ComponentNotIncluded", "Only included components can be marked as skipped.");
        }

        if (visit.Value.Status is not VisitStatusCodes.Open and not VisitStatusCodes.InProgress)
        {
            return Error.Conflict("VisitOperations.VisitNotEditable", "Visit is not editable in its current status.");
        }

        if (executionItem.SkippedComponents.Any(x => x.OfferVersionComponentId == component.Id))
        {
            return Error.Conflict("VisitOperations.ComponentAlreadySkipped", "Component has already been marked as skipped for this execution item.");
        }

        var skippedComponent = visit.Value.RecordSkippedComponent(
            command.VisitExecutionItemId,
            new VisitSkippedComponentDraft(
                component.Id,
                component.ProcedureId,
                component.ProcedureCode,
                component.ProcedureName,
                command.OmissionReasonCode,
                command.Note),
            command.ActorUserId,
            timeProvider.GetUtcNow());
        if (skippedComponent.IsError)
        {
            return skippedComponent.Errors;
        }

        dbContext.Set<VisitSkippedComponent>().Add(skippedComponent.Value);

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
