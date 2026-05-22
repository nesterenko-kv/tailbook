namespace Tailbook.Modules.Reporting.Application.Reporting.Models;

public sealed record EstimateAccuracyReportItemView(Guid VisitId, Guid AppointmentId, DateTimeOffset? ClosedAt, decimal EstimatedAmount, decimal FinalAmount, decimal AmountVariance, int EstimatedServiceMinutes, int EstimatedReservedMinutes, int ActualDurationMinutes, int DurationVarianceMinutes);