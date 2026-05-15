namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record AppointmentCompositionResult(
    Guid? ClientId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    decimal TotalAmount,
    int ServiceMinutes,
    int ReservedMinutes,
    IReadOnlyCollection<AppointmentItemComposition> Items);