namespace Tailbook.BuildingBlocks.Abstractions;

public interface IVisitReportingReadService
{
    Task<IReadOnlyCollection<VisitEstimateAccuracyRow>> ListEstimateAccuracyRowsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<VisitPackagePerformanceRow>> ListPackagePerformanceRowsAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
}

public sealed record VisitEstimateAccuracyRow(
    Guid VisitId,
    Guid AppointmentId,
    DateTime ClosedAtUtc,
    decimal EstimatedAmount,
    decimal AdjustmentAmount,
    decimal FinalAmount,
    int ServiceMinutes,
    int ReservedMinutes);

public sealed record VisitPackagePerformanceRow(
    Guid VisitId,
    Guid AppointmentId,
    DateTime ClosedAtUtc,
    Guid OfferId,
    Guid OfferVersionId,
    string OfferCode,
    string OfferDisplayName,
    int Quantity,
    decimal EstimatedRevenue,
    int PerformedProcedures,
    int SkippedComponents);
