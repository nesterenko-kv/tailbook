namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record GuestBookingIntakeView(
    BookingRequesterSnapshotView? Requester,
    BookingGuestPetSnapshotView? Pet);