namespace Tailbook.Modules.Reporting.Api.Admin.GetEstimateAccuracyReport;

public sealed class GetEstimateAccuracyReportRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
