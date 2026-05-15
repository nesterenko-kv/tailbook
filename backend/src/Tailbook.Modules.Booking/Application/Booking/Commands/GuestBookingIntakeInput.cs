namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record GuestBookingIntakeInput(
    GuestBookingRequesterInput? Requester,
    GuestBookingPetInput? Pet);