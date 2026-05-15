namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record BookingRequestItemView(Guid Id, Guid OfferId, Guid? OfferVersionId, string? ItemType, string? RequestedNotes);