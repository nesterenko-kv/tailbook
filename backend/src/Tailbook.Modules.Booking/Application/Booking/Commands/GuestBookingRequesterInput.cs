namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record GuestBookingRequesterInput(
    string? DisplayName,
    string? Phone,
    string? InstagramHandle,
    string? Email,
    string? PreferredContactMethodCode);