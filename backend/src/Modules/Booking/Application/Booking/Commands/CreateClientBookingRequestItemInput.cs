namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateClientBookingRequestItemInput(Guid OfferId, string? ItemType, string? RequestedNotes);