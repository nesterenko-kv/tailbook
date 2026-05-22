namespace Tailbook.Modules.Reporting.Api.Admin.GetPackagePerformance;

public sealed class GetPackagePerformanceResponse
{
    public IReadOnlyCollection<PackagePerformanceReportItemView> Items { get; set; } = [];
}