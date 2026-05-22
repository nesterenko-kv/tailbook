using ErrorOr;

namespace Tailbook.Modules.Booking.Domain.Aggregates;

public static class AppointmentErrors
{
    public static Error PeriodRequired => Error.Validation(
        code: "Booking.AppointmentPeriodRequired",
        description: "Appointment period is required.");

    public static Error ItemsRequired => Error.Validation(
        code: "Booking.AppointmentItemsRequired",
        description: "Appointment items are required.");

    public static Error AtLeastOneItemRequired => Error.Validation(
        code: "Booking.AppointmentItemsRequired",
        description: "At least one appointment item is required.");

    public static Error IdRequired => Error.Validation(
        code: "Booking.AppointmentIdRequired",
        description: "Appointment id is required.");

    public static Error PetRequired => Error.Validation(
        code: "Booking.AppointmentPetRequired",
        description: "Appointment must reference a pet.");

    public static Error GroomerRequired => Error.Validation(
        code: "Booking.AppointmentGroomerRequired",
        description: "Appointment must reference a groomer.");

    public static Error NotMutable => Error.Conflict(
        code: "Booking.AppointmentNotMutable",
        description: "Appointment is not mutable in its current status.");

    public static Error CancellationReasonRequired => Error.Validation(
        code: "Booking.CancellationReasonRequired",
        description: "Cancellation reason code is required.");

    public static Error CheckInNotAllowed => Error.Conflict(
        code: "Booking.AppointmentCheckInNotAllowed",
        description: "Appointment is not eligible for check-in.");

    public static Error InProgressNotAllowed => Error.Conflict(
        code: "Booking.AppointmentInProgressNotAllowed",
        description: "Appointment is not eligible to enter in-progress state.");

    public static Error CompletionNotAllowed => Error.Conflict(
        code: "Booking.AppointmentCompletionNotAllowed",
        description: "Appointment is not eligible for completion.");

    public static Error ClosureNotAllowed => Error.Conflict(
        code: "Booking.AppointmentClosureNotAllowed",
        description: "Appointment is not eligible for closure.");

    public static Error ItemRequired => Error.Validation(
        code: "Booking.AppointmentItemRequired",
        description: "Appointment item is required.");

    public static Error NotFound => Error.NotFound(
        code: "Booking.AppointmentNotFound",
        description: "Appointment does not exist.");

    public static Error VersionMismatch(int expectedVersionNo, int actualVersionNo) => Error.Conflict(
        code: "Booking.AppointmentVersionMismatch",
        description: $"Appointment version mismatch. Expected {expectedVersionNo}, actual {actualVersionNo}.");
}
