namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record BookingRequestDetailView(
    Guid Id,
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    Guid? PreferredGroomerId,
    string? PreferredGroomerName,
    string? SelectionMode,
    string Channel,
    string Status,
    BookingRequestSubjectView? Subject,
    IReadOnlyCollection<PreferredTimeWindowView> PreferredTimes,
    string? Notes,
    IReadOnlyCollection<BookingRequestItemView> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);