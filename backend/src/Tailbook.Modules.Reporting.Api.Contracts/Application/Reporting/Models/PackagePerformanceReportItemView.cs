namespace Tailbook.Modules.Reporting.Application.Reporting.Models;

public sealed record PackagePerformanceReportItemView(Guid OfferId, string OfferCode, string OfferDisplayName, int BookedCount, int ClosedCount, decimal EstimatedRevenue, decimal FinalRevenue, int SkippedIncludedComponentsCount);