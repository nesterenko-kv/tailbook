namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record BookingRequestListItemView(
    Guid Id,
    Guid? ClientId,
    Guid? PetId,
    Guid? RequestedByContactId,
    Guid? PreferredGroomerId,
    string? SelectionMode,
    string Channel,
    string Status,
    int ItemCount,
    string? PetDisplayName,
    string? RequesterDisplayName,
    string? RequesterPrimaryContact,
    string? PreferredGroomerName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);