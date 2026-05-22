namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record GroomerAppointmentItemView(
    Guid AppointmentItemId,
    string ItemType,
    Guid OfferId,
    Guid OfferVersionId,
    string OfferCode,
    string OfferDisplayName,
    int Quantity,
    int ServiceMinutes,
    int ReservedMinutes,
    IReadOnlyCollection<string> ExecutionPlanSummary);