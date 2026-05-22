namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateBookingRequestItemInput(Guid OfferId, string? ItemType, string? RequestedNotes);