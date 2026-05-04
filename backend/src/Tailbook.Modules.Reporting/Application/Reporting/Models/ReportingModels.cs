namespace Tailbook.Modules.Reporting.Application.Reporting.Models;

public sealed record EstimateAccuracyReportItemView(Guid VisitId, Guid AppointmentId, DateTime? ClosedAtUtc, decimal EstimatedAmount, decimal FinalAmount, decimal AmountVariance, int EstimatedServiceMinutes, int EstimatedReservedMinutes, int ActualDurationMinutes, int DurationVarianceMinutes);
public sealed record PackagePerformanceReportItemView(Guid OfferId, string OfferCode, string OfferDisplayName, int BookedCount, int ClosedCount, decimal EstimatedRevenue, decimal FinalRevenue, int SkippedIncludedComponentsCount);
