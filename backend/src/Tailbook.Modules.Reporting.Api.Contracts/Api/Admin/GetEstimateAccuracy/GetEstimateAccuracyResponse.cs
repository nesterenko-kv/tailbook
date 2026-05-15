namespace Tailbook.Modules.Reporting.Api.Admin.GetEstimateAccuracy;

public sealed class GetEstimateAccuracyResponse
{
    public IReadOnlyCollection<EstimateAccuracyReportItemView> Items { get; set; } = [];
}