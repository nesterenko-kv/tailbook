namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record BookingRequestSubjectView(
    string? PetDisplayName,
    string? AnimalTypeCode,
    string? BreedName,
    string? RequesterDisplayName,
    string? RequesterPrimaryContact,
    string? PreferredGroomerName,
    GuestBookingIntakeView? GuestIntake);