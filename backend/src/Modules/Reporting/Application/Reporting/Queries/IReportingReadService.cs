namespace Tailbook.Modules.Reporting.Application.Reporting.Queries;

public interface IReportingReadService
{
    Task<IReadOnlyCollection<EstimateAccuracyReportItemView>> GetEstimateAccuracyAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PackagePerformanceReportItemView>> GetPackagePerformanceAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
}
