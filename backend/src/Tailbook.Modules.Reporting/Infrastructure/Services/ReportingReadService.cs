using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Reporting.Infrastructure.Services;

public sealed class ReportingReadService(AppDbContext dbContext) : IReportingReadService
{
    public async Task<IReadOnlyCollection<EstimateAccuracyReportItemView>> GetEstimateAccuracyAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await GetEstimateAccuracyInMemoryAsync(from, to, cancellationToken);
        }

        const string sql = """
            SELECT
                v."Id" AS "VisitId",
                v."AppointmentId" AS "AppointmentId",
                v."ClosedAt" AS "ClosedAt",
                COALESCE(SUM(ps."TotalAmount" * ai."Quantity"), 0) AS "EstimatedAmount",
                COALESCE(SUM(ps."TotalAmount" * ai."Quantity"), 0) + COALESCE((SELECT SUM(vpa."Amount" * vpa."Sign") FROM visitops.visit_price_adjustments vpa WHERE vpa."VisitId" = v."Id"), 0) AS "FinalAmount",
                COALESCE((SELECT SUM(vpa."Amount" * vpa."Sign") FROM visitops.visit_price_adjustments vpa WHERE vpa."VisitId" = v."Id"), 0) AS "AmountVariance",
                COALESCE(SUM(ds."ServiceMinutes" * ai."Quantity"), 0) AS "EstimatedServiceMinutes",
                COALESCE(SUM(ds."ReservedMinutes" * ai."Quantity"), 0) AS "EstimatedReservedMinutes",
                CASE
                    WHEN v."StartedAt" IS NOT NULL AND v."CompletedAt" IS NOT NULL THEN GREATEST(0, CAST(EXTRACT(EPOCH FROM (v."CompletedAt" - v."StartedAt")) / 60 AS integer))
                    ELSE 0
                END AS "ActualDurationMinutes",
                CASE
                    WHEN v."StartedAt" IS NOT NULL AND v."CompletedAt" IS NOT NULL THEN GREATEST(0, CAST(EXTRACT(EPOCH FROM (v."CompletedAt" - v."StartedAt")) / 60 AS integer)) - COALESCE(SUM(ds."ServiceMinutes" * ai."Quantity"), 0)
                    ELSE 0 - COALESCE(SUM(ds."ServiceMinutes" * ai."Quantity"), 0)
                END AS "DurationVarianceMinutes"
            FROM visitops.visits v
            JOIN booking.appointment_items ai ON ai."AppointmentId" = v."AppointmentId"
            JOIN booking.price_snapshots ps ON ps."Id" = ai."PriceSnapshotId"
            JOIN booking.duration_snapshots ds ON ds."Id" = ai."DurationSnapshotId"
            WHERE v."Status" = 'Closed'
              AND (@from IS NULL OR v."ClosedAt" >= @from)
              AND (@to IS NULL OR v."ClosedAt" < @to)
            GROUP BY v."Id", v."AppointmentId", v."ClosedAt", v."StartedAt", v."CompletedAt"
            ORDER BY v."ClosedAt" DESC
            """;

        var fromParameter = new NpgsqlParameter("from", from.HasValue ? from.Value : DBNull.Value);
        var toParameter = new NpgsqlParameter("to", to.HasValue ? to.Value : DBNull.Value);

        return await dbContext.Database.SqlQueryRaw<EstimateAccuracyReportItemView>(sql, fromParameter, toParameter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PackagePerformanceReportItemView>> GetPackagePerformanceAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await GetPackagePerformanceInMemoryAsync(from, to, cancellationToken);
        }

        const string sql = """
            WITH visit_item_totals AS (
                SELECT
                    v."Id" AS "VisitId",
                    ai."Id" AS "AppointmentItemId",
                    ai."OfferId",
                    ai."OfferCodeSnapshot",
                    ai."OfferDisplayNameSnapshot",
                    ai."Quantity",
                    ps."TotalAmount",
                    v."Status" AS "VisitStatus",
                    COALESCE(SUM(ps."TotalAmount" * ai."Quantity") OVER (PARTITION BY v."Id"), 0) AS "VisitItemTotal"
                FROM booking.appointment_items ai
                JOIN booking.appointments a ON a."Id" = ai."AppointmentId"
                JOIN booking.price_snapshots ps ON ps."Id" = ai."PriceSnapshotId"
                LEFT JOIN visitops.visits v ON v."AppointmentId" = a."Id"
                WHERE ai."ItemType" = 'Package'
                  AND (@from IS NULL OR a."StartAt" >= @from)
                  AND (@to IS NULL OR a."StartAt" < @to)
            ),
            visit_adjustments AS (
                SELECT
                    vpa."VisitId",
                    SUM(vpa."Amount" * vpa."Sign") AS "AdjustmentValue"
                FROM visitops.visit_price_adjustments vpa
                GROUP BY vpa."VisitId"
            ),
            item_allocated AS (
                SELECT
                    vit."AppointmentItemId",
                    vit."OfferId",
                    vit."OfferCodeSnapshot",
                    vit."OfferDisplayNameSnapshot",
                    vit."Quantity",
                    vit."TotalAmount",
                    vit."VisitStatus",
                    CASE
                        WHEN vit."VisitStatus" = 'Closed' AND vit."VisitItemTotal" > 0 AND va."AdjustmentValue" IS NOT NULL
                        THEN ROUND(va."AdjustmentValue" * (vit."TotalAmount" * vit."Quantity") / vit."VisitItemTotal", 2)
                        ELSE 0
                    END AS "AllocatedAdjustment"
                FROM visit_item_totals vit
                LEFT JOIN visit_adjustments va ON va."VisitId" = vit."VisitId"
            ),
            item_skips AS (
                SELECT
                    vei."AppointmentItemId",
                    COUNT(vsc."Id")::integer AS "SkippedCount"
                FROM visitops.visit_execution_items vei
                LEFT JOIN visitops.visit_skipped_components vsc ON vsc."VisitExecutionItemId" = vei."Id"
                GROUP BY vei."AppointmentItemId"
            )
            SELECT
                ia."OfferId" AS "OfferId",
                ia."OfferCodeSnapshot" AS "OfferCode",
                ia."OfferDisplayNameSnapshot" AS "OfferDisplayName",
                COUNT(*)::integer AS "BookedCount",
                COUNT(*) FILTER (WHERE ia."VisitStatus" = 'Closed')::integer AS "ClosedCount",
                COALESCE(SUM(ia."TotalAmount" * ia."Quantity"), 0) AS "EstimatedRevenue",
                COALESCE(SUM(ia."TotalAmount" * ia."Quantity"), 0) + COALESCE(SUM(ia."AllocatedAdjustment"), 0) AS "FinalRevenue",
                COALESCE(SUM(COALESCE(s."SkippedCount", 0)), 0)::integer AS "SkippedIncludedComponentsCount"
            FROM item_allocated ia
            LEFT JOIN item_skips s ON s."AppointmentItemId" = ia."AppointmentItemId"
            GROUP BY ia."OfferId", ia."OfferCodeSnapshot", ia."OfferDisplayNameSnapshot"
            ORDER BY COUNT(*) DESC, ia."OfferCodeSnapshot"
            """;

        var fromParameter = new NpgsqlParameter("from", from.HasValue ? from.Value : DBNull.Value);
        var toParameter = new NpgsqlParameter("to", to.HasValue ? to.Value : DBNull.Value);

        return await dbContext.Database.SqlQueryRaw<PackagePerformanceReportItemView>(sql, fromParameter, toParameter).ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<EstimateAccuracyReportItemView>> GetEstimateAccuracyInMemoryAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var visits = await dbContext.Set<ReportingVisit>()
            .AsNoTracking()
            .Where(v => v.Status == "Closed")
            .Where(v => !from.HasValue || (v.ClosedAt.HasValue && v.ClosedAt.Value >= from.Value))
            .Where(v => !to.HasValue || (v.ClosedAt.HasValue && v.ClosedAt.Value < to.Value))
            .ToListAsync(cancellationToken);

        var appointmentIds = visits.Select(v => v.AppointmentId).Distinct().ToArray();
        var visitIds = visits.Select(v => v.Id).ToArray();

        var appointmentItems = await dbContext.Set<ReportingAppointmentItem>()
            .AsNoTracking()
            .Where(ai => appointmentIds.Contains(ai.AppointmentId))
            .ToListAsync(cancellationToken);

        var priceSnapshotIds = appointmentItems.Select(ai => ai.PriceSnapshotId).Distinct().ToArray();
        var durationSnapshotIds = appointmentItems.Select(ai => ai.DurationSnapshotId).Distinct().ToArray();

        var priceSnapshots = await dbContext.Set<ReportingPriceSnapshot>()
            .AsNoTracking()
            .Where(ps => priceSnapshotIds.Contains(ps.Id))
            .ToDictionaryAsync(ps => ps.Id, cancellationToken);

        var durationSnapshots = await dbContext.Set<ReportingDurationSnapshot>()
            .AsNoTracking()
            .Where(ds => durationSnapshotIds.Contains(ds.Id))
            .ToDictionaryAsync(ds => ds.Id, cancellationToken);

        var adjustmentByVisitId = await dbContext.Set<ReportingVisitPriceAdjustment>()
            .AsNoTracking()
            .Where(vpa => visitIds.Contains(vpa.VisitId))
            .GroupBy(vpa => vpa.VisitId)
            .Select(g => new { VisitId = g.Key, Sum = g.Sum(x => x.Amount * x.Sign) })
            .ToDictionaryAsync(x => x.VisitId, x => x.Sum, cancellationToken);

        var appointmentItemsByAppointmentId = appointmentItems.GroupBy(ai => ai.AppointmentId).ToDictionary(g => g.Key, g => g.ToArray());

        var report = visits.Select(v =>
        {
            appointmentItemsByAppointmentId.TryGetValue(v.AppointmentId, out var items);
            items ??= [];

            decimal estimatedAmount = 0;
            var estimatedServiceMinutes = 0;
            var estimatedReservedMinutes = 0;

            foreach (var item in items)
            {
                if (priceSnapshots.TryGetValue(item.PriceSnapshotId, out var ps))
                    estimatedAmount += ps.TotalAmount * item.Quantity;
                if (durationSnapshots.TryGetValue(item.DurationSnapshotId, out var ds))
                {
                    estimatedServiceMinutes += ds.ServiceMinutes * item.Quantity;
                    estimatedReservedMinutes += ds.ReservedMinutes * item.Quantity;
                }
            }

            adjustmentByVisitId.TryGetValue(v.Id, out var adjustmentSum);
            var finalAmount = estimatedAmount + adjustmentSum;

            var actualDurationMinutes = v.StartedAt.HasValue && v.CompletedAt.HasValue
                ? Math.Max(0, (int)(v.CompletedAt.Value - v.StartedAt.Value).TotalMinutes)
                : 0;

            return new EstimateAccuracyReportItemView(
                v.Id,
                v.AppointmentId,
                v.ClosedAt,
                estimatedAmount,
                finalAmount,
                adjustmentSum,
                estimatedServiceMinutes,
                estimatedReservedMinutes,
                actualDurationMinutes,
                actualDurationMinutes - estimatedServiceMinutes);
        })
            .OrderByDescending(x => x.ClosedAt)
            .ToArray();

        return report;
    }

    private async Task<IReadOnlyCollection<PackagePerformanceReportItemView>> GetPackagePerformanceInMemoryAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var appointments = await dbContext.Set<ReportingAppointment>()
            .AsNoTracking()
            .Where(a => !from.HasValue || a.StartAt >= from.Value)
            .Where(a => !to.HasValue || a.StartAt < to.Value)
            .ToListAsync(cancellationToken);

        var appointmentIds = appointments.Select(a => a.Id).ToArray();

        var appointmentItems = await dbContext.Set<ReportingAppointmentItem>()
            .AsNoTracking()
            .Where(ai => ai.ItemType == "Package" && appointmentIds.Contains(ai.AppointmentId))
            .ToListAsync(cancellationToken);

        var priceSnapshotIds = appointmentItems.Select(ai => ai.PriceSnapshotId).Distinct().ToArray();
        var priceSnapshots = await dbContext.Set<ReportingPriceSnapshot>()
            .AsNoTracking()
            .Where(ps => priceSnapshotIds.Contains(ps.Id))
            .ToDictionaryAsync(ps => ps.Id, cancellationToken);

        var visits = await dbContext.Set<ReportingVisit>()
            .AsNoTracking()
            .Where(v => appointmentIds.Contains(v.AppointmentId))
            .ToListAsync(cancellationToken);
        var visitByAppointmentId = visits.GroupBy(v => v.AppointmentId).ToDictionary(g => g.Key, g => g.First());
        var visitIds = visits.Select(v => v.Id).ToArray();

        var adjustmentByVisitId = await dbContext.Set<ReportingVisitPriceAdjustment>()
            .AsNoTracking()
            .Where(vpa => visitIds.Contains(vpa.VisitId))
            .GroupBy(vpa => vpa.VisitId)
            .Select(g => new { VisitId = g.Key, Items = g.Select(x => new { x.Amount, x.Sign }).ToList() })
            .ToDictionaryAsync(x => x.VisitId, x => x.Items, cancellationToken);

        var executionItems = await dbContext.Set<ReportingVisitExecutionItem>()
            .AsNoTracking()
            .Where(vei => visitIds.Contains(vei.VisitId))
            .ToListAsync(cancellationToken);
        var executionItemByVisitAndAppointmentItem = executionItems.ToDictionary(x => (x.VisitId, x.AppointmentItemId), x => x);
        var executionItemIds = executionItems.Select(x => x.Id).ToArray();

        var skippedCountsByExecutionItemId = await dbContext.Set<ReportingVisitSkippedComponent>()
            .AsNoTracking()
            .Where(vsc => executionItemIds.Contains(vsc.VisitExecutionItemId))
            .GroupBy(vsc => vsc.VisitExecutionItemId)
            .Select(g => new { ExecutionItemId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ExecutionItemId, x => x.Count, cancellationToken);

        var report = appointmentItems
            .GroupBy(ai => new { ai.OfferId, ai.OfferCodeSnapshot, ai.OfferDisplayNameSnapshot })
            .Select(group =>
            {
                var bookedCount = 0;
                var closedCount = 0;
                decimal estimatedRevenue = 0;
                decimal finalRevenue = 0;
                var skippedIncludedComponentsCount = 0;
                var visitItemEstimates = new Dictionary<Guid, List<(decimal Estimated, Guid AppointmentItemId)>>();

                foreach (var item in group)
                {
                    bookedCount++;

                    priceSnapshots.TryGetValue(item.PriceSnapshotId, out var priceSnapshot);
                    var lineEstimated = (priceSnapshot?.TotalAmount ?? 0m) * item.Quantity;
                    estimatedRevenue += lineEstimated;

                    if (visitByAppointmentId.TryGetValue(item.AppointmentId, out var visit))
                    {
                        if (visit.Status == "Closed")
                            closedCount++;

                        if (!visitItemEstimates.ContainsKey(visit.Id))
                            visitItemEstimates[visit.Id] = [];
                        visitItemEstimates[visit.Id].Add((lineEstimated, item.Id));

                        if (executionItemByVisitAndAppointmentItem.TryGetValue((visit.Id, item.Id), out var executionItem) &&
                            skippedCountsByExecutionItemId.TryGetValue(executionItem.Id, out var skippedCount))
                            skippedIncludedComponentsCount += skippedCount;
                    }
                }

                finalRevenue = estimatedRevenue;

                foreach (var (visitId, items) in visitItemEstimates)
                {
                    if (!adjustmentByVisitId.TryGetValue(visitId, out var adjustments))
                        continue;

                    var adjustmentSum = adjustments.Sum(a => a.Amount * a.Sign);
                    if (adjustmentSum == 0)
                        continue;

                    var visitItemTotal = items.Sum(i => i.Estimated);
                    if (visitItemTotal <= 0)
                        continue;

                    foreach (var (lineEst, _) in items)
                    {
                        var share = lineEst / visitItemTotal;
                        finalRevenue += Math.Round(adjustmentSum * share, 2, MidpointRounding.AwayFromZero);
                    }
                }

                return new PackagePerformanceReportItemView(
                    group.Key.OfferId,
                    group.Key.OfferCodeSnapshot,
                    group.Key.OfferDisplayNameSnapshot,
                    bookedCount,
                    closedCount,
                    estimatedRevenue,
                    finalRevenue,
                    skippedIncludedComponentsCount);
            })
            .OrderByDescending(x => x.BookedCount)
            .ThenBy(x => x.OfferCode)
            .ToArray();

        return report;
    }
}
