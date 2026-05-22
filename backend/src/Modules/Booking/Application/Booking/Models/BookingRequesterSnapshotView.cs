namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record BookingRequesterSnapshotView(
    string? DisplayName,
    string? Phone,
    string? InstagramHandle,
    string? Email,
    string? PreferredContactMethodCode)
{
    public string? PrimaryContactDisplay =>
        !string.IsNullOrWhiteSpace(Phone) ? Phone :
        !string.IsNullOrWhiteSpace(InstagramHandle) ? InstagramHandle :
        !string.IsNullOrWhiteSpace(Email) ? Email : null;
}