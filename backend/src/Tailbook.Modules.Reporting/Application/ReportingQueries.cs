using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Reporting.Domain;

namespace Tailbook.Modules.Reporting.Application;

public sealed class ReportingQueries(AppDbContext dbContext)
{
    public async Task<IReadOnlyCollection<EstimateAccuracyReportItemView>> GetEstimateAccuracyAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await GetEstimateAccuracyInMemoryAsync(fromUtc, toUtc, cancellationToken);
        }

        const string sql = """
            SELECT
                v."Id" AS "VisitId",
                v."AppointmentId" AS "AppointmentId",
                v."ClosedAtUtc" AS "ClosedAtUtc",
                COALESCE(SUM(ps."TotalAmount" * ai."Quantity"), 0) AS "EstimatedAmount",
                COALESCE(SUM(ps."TotalAmount" * ai."Quantity"), 0) + COALESCE((SELECT SUM(vpa."Amount" * vpa."Sign") FROM visitops.visit_price_adjustments vpa WHERE vpa."VisitId" = v."Id"), 0) AS "FinalAmount",
                COALESCE((SELECT SUM(vpa."Amount" * vpa."Sign") FROM visitops.visit_price_adjustments vpa WHERE vpa."VisitId" = v."Id"), 0) AS "AmountVariance",
                COALESCE(SUM(ds."ServiceMinutes" * ai."Quantity"), 0) AS "EstimatedServiceMinutes",
                COALESCE(SUM(ds."ReservedMinutes" * ai."Quantity"), 0) AS "EstimatedReservedMinutes",
                CASE
                    WHEN v."StartedAtUtc" IS NOT NULL AND v."CompletedAtUtc" IS NOT NULL THEN GREATEST(0, CAST(EXTRACT(EPOCH FROM (v."CompletedAtUtc" - v."StartedAtUtc")) / 60 AS integer))
                    ELSE 0
                END AS "ActualDurationMinutes",
                CASE
                    WHEN v."StartedAtUtc" IS NOT NULL AND v."CompletedAtUtc" IS NOT NULL THEN GREATEST(0, CAST(EXTRACT(EPOCH FROM (v."CompletedAtUtc" - v."StartedAtUtc")) / 60 AS integer)) - COALESCE(SUM(ds."ServiceMinutes" * ai."Quantity"), 0)
                    ELSE 0 - COALESCE(SUM(ds."ServiceMinutes" * ai."Quantity"), 0)
                END AS "DurationVarianceMinutes"
            FROM visitops.visits v
            JOIN booking.appointment_items ai ON ai."AppointmentId" = v."AppointmentId"
            JOIN booking.price_snapshots ps ON ps."Id" = ai."PriceSnapshotId"
            JOIN booking.duration_snapshots ds ON ds."Id" = ai."DurationSnapshotId"
            WHERE v."Status" = 'Closed'
              AND (@fromUtc IS NULL OR v."ClosedAtUtc" >= @fromUtc)
              AND (@toUtc IS NULL OR v."ClosedAtUtc" < @toUtc)
            GROUP BY v."Id", v."AppointmentId", v."ClosedAtUtc", v."StartedAtUtc", v."CompletedAtUtc"
            ORDER BY v."ClosedAtUtc" DESC
            """;

        var fromParameter = new Npgsql.NpgsqlParameter("fromUtc", fromUtc.HasValue ? fromUtc.Value : DBNull.Value);
        var toParameter = new Npgsql.NpgsqlParameter("toUtc", toUtc.HasValue ? toUtc.Value : DBNull.Value);

        return await dbContext.Database.SqlQueryRaw<EstimateAccuracyReportItemView>(sql, fromParameter, toParameter).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PackagePerformanceReportItemView>> GetPackagePerformanceAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await GetPackagePerformanceInMemoryAsync(fromUtc, toUtc, cancellationToken);
        }

        const string sql = """
            WITH item_base AS (
                SELECT
                    ai."Id" AS "AppointmentItemId",
                    ai."OfferId",
                    ai."OfferCodeSnapshot",
                    ai."OfferDisplayNameSnapshot",
                    ai."Quantity",
                    ps."TotalAmount",
                    v."Id" AS "VisitId",
                    v."Status" AS "VisitStatus",
                    COALESCE(adj."AdjustmentValue", 0) AS "AdjustmentValue",
                    COALESCE(skips."SkippedCount", 0) AS "SkippedCount"
                FROM booking.appointment_items ai
                JOIN booking.appointments a ON a."Id" = ai."AppointmentId"
                JOIN booking.price_snapshots ps ON ps."Id" = ai."PriceSnapshotId"
                LEFT JOIN visitops.visits v ON v."AppointmentId" = a."Id"
                LEFT JOIN LATERAL (
                    SELECT COALESCE(SUM(vpa."Amount" * vpa."Sign"), 0) AS "AdjustmentValue"
                    FROM visitops.visit_price_adjustments vpa
                    WHERE vpa."VisitId" = v."Id"
                ) adj ON TRUE
                LEFT JOIN LATERAL (
                    SELECT COUNT(vsc."Id")::integer AS "SkippedCount"
                    FROM visitops.visit_execution_items vei
                    JOIN visitops.visit_skipped_components vsc ON vsc."VisitExecutionItemId" = vei."Id"
                    WHERE vei."VisitId" = v."Id" AND vei."AppointmentItemId" = ai."Id"
                ) skips ON TRUE
                WHERE ai."ItemType" = 'Package'
                  AND (@fromUtc IS NULL OR a."StartAtUtc" >= @fromUtc)
                  AND (@toUtc IS NULL OR a."StartAtUtc" < @toUtc)
            )
            SELECT
                "OfferId" AS "OfferId",
                "OfferCodeSnapshot" AS "OfferCode",
                "OfferDisplayNameSnapshot" AS "OfferDisplayName",
                COUNT(*)::integer AS "BookedCount",
                COUNT(*) FILTER (WHERE "VisitStatus" = 'Closed')::integer AS "ClosedCount",
                COALESCE(SUM("TotalAmount" * "Quantity"), 0) AS "EstimatedRevenue",
                COALESCE(SUM("TotalAmount" * "Quantity"), 0) + COALESCE(SUM(CASE WHEN "VisitStatus" = 'Closed' THEN "AdjustmentValue" ELSE 0 END), 0) AS "FinalRevenue",
                COALESCE(SUM("SkippedCount"), 0)::integer AS "SkippedIncludedComponentsCount"
            FROM item_base
            GROUP BY "OfferId", "OfferCodeSnapshot", "OfferDisplayNameSnapshot"
            ORDER BY COUNT(*) DESC, "OfferCodeSnapshot"
            """;

        var fromParameter = new Npgsql.NpgsqlParameter("fromUtc", fromUtc.HasValue ? fromUtc.Value : DBNull.Value);
        var toParameter = new Npgsql.NpgsqlParameter("toUtc", toUtc.HasValue ? toUtc.Value : DBNull.Value);

        return await dbContext.Database.SqlQueryRaw<PackagePerformanceReportItemView>(sql, fromParameter, toParameter).ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<EstimateAccuracyReportItemView>> GetEstimateAccuracyInMemoryAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var visits = await dbContext.Set<ReportingVisit>()
            .AsNoTracking()
            .Where(v => v.Status == "Closed")
            .Where(v => !fromUtc.HasValue || (v.ClosedAtUtc.HasValue && v.ClosedAtUtc.Value >= fromUtc.Value))
            .Where(v => !toUtc.HasValue || (v.ClosedAtUtc.HasValue && v.ClosedAtUtc.Value < toUtc.Value))
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

            var actualDurationMinutes = v.StartedAtUtc.HasValue && v.CompletedAtUtc.HasValue
                ? Math.Max(0, (int)(v.CompletedAtUtc.Value - v.StartedAtUtc.Value).TotalMinutes)
                : 0;

            return new EstimateAccuracyReportItemView(
                v.Id,
                v.AppointmentId,
                v.ClosedAtUtc,
                estimatedAmount,
                finalAmount,
                adjustmentSum,
                estimatedServiceMinutes,
                estimatedReservedMinutes,
                actualDurationMinutes,
                actualDurationMinutes - estimatedServiceMinutes);
        })
            .OrderByDescending(x => x.ClosedAtUtc)
            .ToArray();

        return report;
    }

    private async Task<IReadOnlyCollection<PackagePerformanceReportItemView>> GetPackagePerformanceInMemoryAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var appointments = await dbContext.Set<ReportingAppointment>()
            .AsNoTracking()
            .Where(a => !fromUtc.HasValue || a.StartAtUtc >= fromUtc.Value)
            .Where(a => !toUtc.HasValue || a.StartAtUtc < toUtc.Value)
            .ToListAsync(cancellationToken);

        var appointmentById = appointments.ToDictionary(a => a.Id);
        var appointmentIds = appointmentById.Keys.ToArray();

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
            .Select(g => new { VisitId = g.Key, Sum = g.Sum(x => x.Amount * x.Sign) })
            .ToDictionaryAsync(x => x.VisitId, x => x.Sum, cancellationToken);

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

                foreach (var item in group)
                {
                    bookedCount++;

                    priceSnapshots.TryGetValue(item.PriceSnapshotId, out var priceSnapshot);
                    var lineEstimated = (priceSnapshot?.TotalAmount ?? 0m) * item.Quantity;
                    estimatedRevenue += lineEstimated;
                    finalRevenue += lineEstimated;

                    if (visitByAppointmentId.TryGetValue(item.AppointmentId, out var visit))
                    {
                        if (visit.Status == "Closed")
                            closedCount++;

                        if (visit.Status == "Closed" && adjustmentByVisitId.TryGetValue(visit.Id, out var adjustment))
                            finalRevenue += adjustment;

                        if (executionItemByVisitAndAppointmentItem.TryGetValue((visit.Id, item.Id), out var executionItem) &&
                            skippedCountsByExecutionItemId.TryGetValue(executionItem.Id, out var skippedCount))
                            skippedIncludedComponentsCount += skippedCount;
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

public sealed record EstimateAccuracyReportItemView(Guid VisitId, Guid AppointmentId, DateTime? ClosedAtUtc, decimal EstimatedAmount, decimal FinalAmount, decimal AmountVariance, int EstimatedServiceMinutes, int EstimatedReservedMinutes, int ActualDurationMinutes, int DurationVarianceMinutes);
public sealed record PackagePerformanceReportItemView(Guid OfferId, string OfferCode, string OfferDisplayName, int BookedCount, int ClosedCount, decimal EstimatedRevenue, decimal FinalRevenue, int SkippedIncludedComponentsCount);
