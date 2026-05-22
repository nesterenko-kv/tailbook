namespace Tailbook.Modules.Reporting.Api.Admin.GetEstimateAccuracy;

public sealed class GetEstimateAccuracyRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}