namespace Tailbook.Modules.Reporting.Api.Admin.GetPackagePerformance;

public sealed class GetPackagePerformanceRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}