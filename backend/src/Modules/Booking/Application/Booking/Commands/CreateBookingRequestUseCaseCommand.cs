using ErrorOr;
using FastEndpoints;

namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CreateBookingRequestUseCaseCommand(
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    string? Channel,
    string? Notes,
    IReadOnlyCollection<PreferredTimeWindowInput> PreferredTimes,
    IReadOnlyCollection<CreateBookingRequestItemInput> Items,
    Guid? ActorUserId,
    Guid? PreferredGroomerId = null,
    string? SelectionMode = null,
    GuestBookingIntakeInput? GuestIntake = null,
    string? InitialStatus = null) : ICommand<ErrorOr<BookingRequestDetailView>>;
