namespace Tailbook.Modules.Reporting.Application.Reporting.Queries;

public interface IReportingReadService
{
    Task<IReadOnlyCollection<EstimateAccuracyReportItemView>> GetEstimateAccuracyAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PackagePerformanceReportItemView>> GetPackagePerformanceAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken);
}
