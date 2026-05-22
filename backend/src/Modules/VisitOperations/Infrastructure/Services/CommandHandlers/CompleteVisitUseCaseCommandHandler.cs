using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

public sealed class CompleteVisitUseCaseCommandHandler(
    AppDbContext dbContext,
    IVisitReadService visitReadService,
    IAppointmentVisitService appointmentVisitService,
    IVisitCatalogReadService visitCatalogReadService,
    IAuditTrailService auditTrailService,
    TimeProvider timeProvider)
    : ICommandHandler<CompleteVisitUseCaseCommand, ErrorOr<VisitDetailView>>
{
    public async Task<ErrorOr<VisitDetailView>> ExecuteAsync(CompleteVisitUseCaseCommand command, CancellationToken ct = default)
    {
        var visit = await LoadVisitAggregateAsync(command.VisitId, ct);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        if (visit.Value.Status is not VisitStatusCodes.Open and not VisitStatusCodes.InProgress)
        {
            return Error.Conflict("VisitOperations.VisitCompletionFailed", "Visit is not eligible for completion.");
        }

        var componentsAccountedFor = await EnsureDefaultExpectedComponentsAccountedForAsync(visit.Value.Id, ct);
        if (componentsAccountedFor.IsError)
        {
            return componentsAccountedFor.Errors;
        }

        var markCompletedResult = await appointmentVisitService.MarkCompletedAsync(visit.Value.AppointmentId, command.ActorUserId, ct);
        if (markCompletedResult.IsError)
        {
            return markCompletedResult.Errors;
        }

        var completeResult = visit.Value.Complete(command.ActorUserId, timeProvider.GetUtcNow());
        if (completeResult.IsError)
        {
            return completeResult.Errors;
        }

        await dbContext.SaveChangesAsync(ct);
        await auditTrailService.RecordAsync(
            "visitops",
            "visit",
            visit.Value.Id.ToString("D"),
            "COMPLETE",
            command.ActorUserId,
            null,
            JsonSerializer.Serialize(new { visit.Value.Status, visit.Value.CompletedAt }),
            ct);

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

    private async Task<ErrorOr<Success>> EnsureDefaultExpectedComponentsAccountedForAsync(Guid visitId, CancellationToken cancellationToken)
    {
        var executionItems = await dbContext.Set<VisitExecutionItem>()
            .Where(x => x.VisitId == visitId)
            .ToListAsync(cancellationToken);
        var executionItemIds = executionItems.Select(x => x.Id).ToArray();
        var performedProcedures = await dbContext.Set<VisitPerformedProcedure>()
            .Where(x => executionItemIds.Contains(x.VisitExecutionItemId))
            .ToListAsync(cancellationToken);
        var performedProcedureIdsByItem = performedProcedures
            .GroupBy(x => x.VisitExecutionItemId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ProcedureId).ToHashSet());
        var skippedComponents = await dbContext.Set<VisitSkippedComponent>()
            .Where(x => executionItemIds.Contains(x.VisitExecutionItemId))
            .ToListAsync(cancellationToken);
        var skippedComponentIdsByItem = skippedComponents
            .GroupBy(x => x.VisitExecutionItemId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.OfferVersionComponentId).ToHashSet());

        foreach (var executionItem in executionItems)
        {
            var components = await visitCatalogReadService.GetIncludedComponentsAsync(executionItem.OfferVersionId, cancellationToken);
            foreach (var component in components.Where(x => x.DefaultExpected))
            {
                var wasPerformed = performedProcedureIdsByItem.TryGetValue(executionItem.Id, out var performedProcedureIds)
                                   && performedProcedureIds.Contains(component.ProcedureId);
                var wasSkipped = skippedComponentIdsByItem.TryGetValue(executionItem.Id, out var skippedComponentIds)
                                 && skippedComponentIds.Contains(component.Id);
                if (!wasPerformed && !wasSkipped)
                {
                    return VisitErrors.DefaultComponentIncomplete(component.ProcedureName);
                }
            }
        }

        return Result.Success;
    }

    private async Task<ErrorOr<VisitDetailView>> ReadVisitAsync(Guid visitId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var visit = await visitReadService.GetVisitAsync(visitId, actorUserId, cancellationToken);
        return visit is null
            ? Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.")
            : visit;
    }
}
