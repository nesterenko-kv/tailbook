namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateClientBookingRequestInput(
    Guid PetId,
    string? Notes,
    IReadOnlyCollection<PreferredTimeWindowInput> PreferredTimes,
    IReadOnlyCollection<CreateClientBookingRequestItemInput> Items);