using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

public sealed class ApplyVisitPriceAdjustmentUseCaseCommandHandler(
    AppDbContext dbContext,
    IVisitReadService visitReadService,
    IAuditTrailService auditTrailService,
    IOutboxPublisher outboxPublisher,
    TimeProvider timeProvider)
    : ICommandHandler<ApplyVisitPriceAdjustmentUseCaseCommand, ErrorOr<VisitDetailView>>
{
    public async Task<ErrorOr<VisitDetailView>> ExecuteAsync(ApplyVisitPriceAdjustmentUseCaseCommand command, CancellationToken ct = default)
    {
        var visit = await LoadVisitAggregateAsync(command.VisitId, ct);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        if (visit.Value.Status is not VisitStatusCodes.Open and not VisitStatusCodes.InProgress and not VisitStatusCodes.AwaitingFinalization)
        {
            return Error.Conflict("VisitOperations.VisitNotEditable", "Visit is not editable in its current status.");
        }

        if (command.Sign is not -1 and not 1)
        {
            return Error.Validation("VisitOperations.InvalidAdjustmentSign", "Adjustment sign must be either -1 or 1.");
        }

        if (command.Amount <= 0)
        {
            return Error.Validation("VisitOperations.InvalidAdjustmentAmount", "Adjustment amount must be greater than zero.");
        }

        var roundedAmount = decimal.Round(command.Amount, 2, MidpointRounding.AwayFromZero);
        if (visit.Value.FinalTotalAmount + (roundedAmount * command.Sign) < 0)
        {
            return Error.Validation("VisitOperations.NegativeFinalTotal", "Visit final total cannot be negative.");
        }

        var adjustment = visit.Value.ApplyPriceAdjustment(
            new VisitPriceAdjustmentDraft(command.Sign, command.Amount, command.ReasonCode, command.Note, command.TargetItemId),
            command.ActorUserId,
            timeProvider.GetUtcNow());
        if (adjustment.IsError)
        {
            return adjustment.Errors;
        }

        dbContext.Set<VisitPriceAdjustment>().Add(adjustment.Value);

        await outboxPublisher.PublishAsync("visitops", "FinalPriceAdjusted", new
        {
            visitId = visit.Value.Id,
            status = visit.Value.Status,
            sign = adjustment.Value.Sign,
            amount = adjustment.Value.Amount,
            reasonCode = adjustment.Value.ReasonCode
        }, ct);
        await dbContext.SaveChangesAsync(ct);
        await auditTrailService.RecordAsync(
            "visitops",
            "visit",
            visit.Value.Id.ToString("D"),
            "APPLY_ADJUSTMENT",
            command.ActorUserId,
            null,
            JsonSerializer.Serialize(new { adjustment.Value.Sign, adjustment.Value.Amount, adjustment.Value.ReasonCode }),
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

    private async Task<ErrorOr<VisitDetailView>> ReadVisitAsync(Guid visitId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var visit = await visitReadService.GetVisitAsync(visitId, actorUserId, cancellationToken);
        return visit is null
            ? Error.NotFound("VisitOperations.VisitNotFound", "Visit does not exist.")
            : visit;
    }
}
