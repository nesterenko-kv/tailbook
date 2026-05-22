namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record ClientBookableOfferView(
    Guid Id,
    string OfferType,
    string DisplayName,
    string Currency,
    decimal PriceAmount,
    int ServiceMinutes,
    int ReservedMinutes);