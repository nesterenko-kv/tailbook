namespace Tailbook.BuildingBlocks.Abstractions;

public interface IVisitReportingReadService
{
    Task<IReadOnlyCollection<VisitEstimateAccuracyRow>> ListEstimateAccuracyRowsAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<VisitPackagePerformanceRow>> ListPackagePerformanceRowsAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
}

public sealed record VisitEstimateAccuracyRow(
    Guid VisitId,
    Guid AppointmentId,
    DateTimeOffset ClosedAt,
    decimal EstimatedAmount,
    decimal AdjustmentAmount,
    decimal FinalAmount,
    int ServiceMinutes,
    int ReservedMinutes);

public sealed record VisitPackagePerformanceRow(
    Guid VisitId,
    Guid AppointmentId,
    DateTimeOffset ClosedAt,
    Guid OfferId,
    Guid OfferVersionId,
    string OfferCode,
    string OfferDisplayName,
    int Quantity,
    decimal EstimatedRevenue,
    int PerformedProcedures,
    int SkippedComponents);
