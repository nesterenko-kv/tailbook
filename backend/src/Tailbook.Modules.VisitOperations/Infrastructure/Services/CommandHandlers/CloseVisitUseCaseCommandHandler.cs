using System.Text.Json;
using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services.CommandHandlers;

public sealed class CloseVisitUseCaseCommandHandler(
    AppDbContext dbContext,
    IVisitReadService visitReadService,
    IAppointmentVisitService appointmentVisitService,
    IAuditTrailService auditTrailService,
    IOutboxPublisher outboxPublisher,
    TimeProvider timeProvider)
    : ICommandHandler<CloseVisitUseCaseCommand, ErrorOr<VisitDetailView>>
{
    public async Task<ErrorOr<VisitDetailView>> ExecuteAsync(CloseVisitUseCaseCommand command, CancellationToken ct = default)
    {
        var visit = await LoadVisitAggregateAsync(command.VisitId, ct);
        if (visit.IsError)
        {
            return visit.Errors;
        }

        if (visit.Value.Status != VisitStatusCodes.AwaitingFinalization)
        {
            return Error.Conflict("VisitOperations.VisitCloseFailed", "Visit is not eligible for closure.");
        }

        var markClosedResult = await appointmentVisitService.MarkClosedAsync(visit.Value.AppointmentId, command.ActorUserId, ct);
        if (markClosedResult.IsError)
        {
            return markClosedResult.Errors;
        }

        var closeResult = visit.Value.Close(command.ActorUserId, timeProvider.GetUtcNow());
        if (closeResult.IsError)
        {
            return closeResult.Errors;
        }

        await outboxPublisher.PublishAsync("visitops", "VisitClosed", new
        {
            visitId = visit.Value.Id,
            appointmentId = visit.Value.AppointmentId,
            status = visit.Value.Status,
            finalTotalAmount = visit.Value.FinalTotalAmount,
            closedAt = visit.Value.ClosedAt
        }, ct);
        await dbContext.SaveChangesAsync(ct);
        await auditTrailService.RecordAsync(
            "visitops",
            "visit",
            visit.Value.Id.ToString("D"),
            "CLOSE",
            command.ActorUserId,
            null,
            JsonSerializer.Serialize(new { visit.Value.Status, visit.Value.FinalTotalAmount }),
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
