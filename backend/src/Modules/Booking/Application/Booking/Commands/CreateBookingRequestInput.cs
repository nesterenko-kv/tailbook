namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateBookingRequestInput(
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    string? Channel,
    string? Notes,
    IReadOnlyCollection<PreferredTimeWindowInput> PreferredTimes,
    IReadOnlyCollection<CreateBookingRequestItemInput> Items,
    Guid? PreferredGroomerId = null,
    string? SelectionMode = null,
    GuestBookingIntakeInput? GuestIntake = null,
    string? InitialStatus = null);