using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Reporting.Application;

public sealed class ReportingQueries(AppDbContext dbContext)
{
    public async Task<IReadOnlyCollection<EstimateAccuracyReportItemView>> GetEstimateAccuracyAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return [];
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
            return [];
        }

        const string sql = """
            SELECT
                ai."OfferId" AS "OfferId",
                ai."OfferCodeSnapshot" AS "OfferCode",
                ai."OfferDisplayNameSnapshot" AS "OfferDisplayName",
                COUNT(*)::integer AS "BookedCount",
                COUNT(*) FILTER (WHERE v."Status" = 'Closed')::integer AS "ClosedCount",
                COALESCE(SUM(ps."TotalAmount" * ai."Quantity"), 0) AS "EstimatedRevenue",
                COALESCE(SUM(ps."TotalAmount" * ai."Quantity"), 0) + COALESCE(SUM(adj."AdjustmentValue"), 0) AS "FinalRevenue",
                COUNT(vsc."Id")::integer AS "SkippedIncludedComponentsCount"
            FROM booking.appointment_items ai
            JOIN booking.appointments a ON a."Id" = ai."AppointmentId"
            JOIN booking.price_snapshots ps ON ps."Id" = ai."PriceSnapshotId"
            LEFT JOIN visitops.visits v ON v."AppointmentId" = a."Id"
            LEFT JOIN visitops.visit_execution_items vei ON vei."VisitId" = v."Id" AND vei."AppointmentItemId" = ai."Id"
            LEFT JOIN visitops.visit_skipped_components vsc ON vsc."VisitExecutionItemId" = vei."Id"
            LEFT JOIN LATERAL (
                SELECT COALESCE(SUM(vpa."Amount" * vpa."Sign"), 0) AS "AdjustmentValue"
                FROM visitops.visit_price_adjustments vpa
                WHERE vpa."VisitId" = v."Id"
            ) adj ON TRUE
            WHERE ai."ItemType" = 'Package'
              AND (@fromUtc IS NULL OR a."StartAtUtc" >= @fromUtc)
              AND (@toUtc IS NULL OR a."StartAtUtc" < @toUtc)
            GROUP BY ai."OfferId", ai."OfferCodeSnapshot", ai."OfferDisplayNameSnapshot"
            ORDER BY COUNT(*) DESC, ai."OfferCodeSnapshot"
            """;

        var fromParameter = new Npgsql.NpgsqlParameter("fromUtc", fromUtc.HasValue ? fromUtc.Value : DBNull.Value);
        var toParameter = new Npgsql.NpgsqlParameter("toUtc", toUtc.HasValue ? toUtc.Value : DBNull.Value);

        return await dbContext.Database.SqlQueryRaw<PackagePerformanceReportItemView>(sql, fromParameter, toParameter).ToListAsync(cancellationToken);
    }
}

public sealed record EstimateAccuracyReportItemView(Guid VisitId, Guid AppointmentId, DateTime? ClosedAtUtc, decimal EstimatedAmount, decimal FinalAmount, decimal AmountVariance, int EstimatedServiceMinutes, int EstimatedReservedMinutes, int ActualDurationMinutes, int DurationVarianceMinutes);
public sealed record PackagePerformanceReportItemView(Guid OfferId, string OfferCode, string OfferDisplayName, int BookedCount, int ClosedCount, decimal EstimatedRevenue, decimal FinalRevenue, int SkippedIncludedComponentsCount);
