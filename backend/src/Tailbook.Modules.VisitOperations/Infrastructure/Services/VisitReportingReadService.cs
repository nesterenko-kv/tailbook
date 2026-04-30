using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.VisitOperations.Contracts;

namespace Tailbook.Modules.VisitOperations.Infrastructure.Services;

public sealed class VisitReportingReadService(AppDbContext dbContext) : IVisitReportingReadService
{
    public async Task<IReadOnlyCollection<VisitEstimateAccuracyRow>> ListEstimateAccuracyRowsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var visitsQuery = dbContext.Set<Visit>()
            .Where(x => x.Status == VisitStatusCodes.Closed && x.ClosedAtUtc != null);

        if (fromUtc.HasValue) visitsQuery = visitsQuery.Where(x => x.ClosedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) visitsQuery = visitsQuery.Where(x => x.ClosedAtUtc <= toUtc.Value);

        var visits = await visitsQuery.OrderByDescending(x => x.ClosedAtUtc).ToListAsync(cancellationToken);
        var visitIds = visits.Select(x => x.Id).ToArray();

        var executionItems = await dbContext.Set<VisitExecutionItem>()
            .Where(x => visitIds.Contains(x.VisitId))
            .ToListAsync(cancellationToken);

        var adjustments = await dbContext.Set<VisitPriceAdjustment>()
            .Where(x => visitIds.Contains(x.VisitId))
            .ToListAsync(cancellationToken);

        return visits.Select(visit =>
        {
            var visitItems = executionItems.Where(x => x.VisitId == visit.Id).ToArray();
            var visitAdjustments = adjustments.Where(x => x.VisitId == visit.Id).ToArray();
            var estimate = visitItems.Sum(x => x.PriceAmountSnapshot * x.Quantity);
            var serviceMinutes = visitItems.Sum(x => x.ServiceMinutesSnapshot * x.Quantity);
            var reservedMinutes = visitItems.Sum(x => x.ReservedMinutesSnapshot * x.Quantity);
            var adjustmentTotal = visitAdjustments.Sum(x => x.Amount * x.Sign);
            var finalTotal = estimate + adjustmentTotal;
            return new VisitEstimateAccuracyRow(
                visit.Id,
                visit.AppointmentId,
                visit.ClosedAtUtc!.Value,
                estimate,
                adjustmentTotal,
                finalTotal,
                serviceMinutes,
                reservedMinutes);
        }).ToArray();
    }

    public async Task<IReadOnlyCollection<VisitPackagePerformanceRow>> ListPackagePerformanceRowsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var visitsQuery = dbContext.Set<Visit>()
            .Where(x => x.Status == VisitStatusCodes.Closed && x.ClosedAtUtc != null);

        if (fromUtc.HasValue) visitsQuery = visitsQuery.Where(x => x.ClosedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) visitsQuery = visitsQuery.Where(x => x.ClosedAtUtc <= toUtc.Value);

        var visits = await visitsQuery.ToListAsync(cancellationToken);
        var visitIds = visits.Select(x => x.Id).ToArray();
        var items = await dbContext.Set<VisitExecutionItem>()
            .Where(x => visitIds.Contains(x.VisitId) && x.ItemType == "Package")
            .ToListAsync(cancellationToken);
        var itemIds = items.Select(x => x.Id).ToArray();
        var performed = await dbContext.Set<VisitPerformedProcedure>()
            .Where(x => itemIds.Contains(x.VisitExecutionItemId))
            .ToListAsync(cancellationToken);
        var skipped = await dbContext.Set<VisitSkippedComponent>()
            .Where(x => itemIds.Contains(x.VisitExecutionItemId))
            .ToListAsync(cancellationToken);

        return items.Select(item =>
        {
            var visit = visits.Single(x => x.Id == item.VisitId);
            return new VisitPackagePerformanceRow(
                item.VisitId,
                visit.AppointmentId,
                visit.ClosedAtUtc!.Value,
                item.OfferId,
                item.OfferVersionId,
                item.OfferCodeSnapshot,
                item.OfferDisplayNameSnapshot,
                item.Quantity,
                item.PriceAmountSnapshot * item.Quantity,
                performed.Count(x => x.VisitExecutionItemId == item.Id),
                skipped.Count(x => x.VisitExecutionItemId == item.Id));
        }).ToArray();
    }
}
