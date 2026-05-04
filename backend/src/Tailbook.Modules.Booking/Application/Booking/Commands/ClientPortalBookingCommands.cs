namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateClientBookingRequestCommand(
    Guid PetId,
    string? Notes,
    IReadOnlyCollection<PreferredTimeWindowCommand> PreferredTimes,
    IReadOnlyCollection<CreateClientBookingRequestItemCommand> Items);

public sealed record CreateClientBookingRequestItemCommand(Guid OfferId, string? ItemType, string? RequestedNotes);
