namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record ClientAppointmentItemView(
    Guid Id,
    string ItemType,
    string OfferDisplayName,
    decimal PriceAmount,
    int ServiceMinutes,
    int ReservedMinutes);