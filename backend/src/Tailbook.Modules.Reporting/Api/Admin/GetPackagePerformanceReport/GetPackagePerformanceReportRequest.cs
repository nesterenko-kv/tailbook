namespace Tailbook.Modules.Reporting.Api.Admin.GetPackagePerformanceReport;

public sealed class GetPackagePerformanceReportRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
