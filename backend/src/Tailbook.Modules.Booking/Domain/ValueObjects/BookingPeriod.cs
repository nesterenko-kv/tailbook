using ErrorOr;

namespace Tailbook.Modules.Booking.Domain.ValueObjects;

public sealed record BookingPeriod
{
    private BookingPeriod(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        StartAt = startAt;
        EndAt = endAt;
    }

    public DateTimeOffset StartAt { get; }
    public DateTimeOffset EndAt { get; }

    public static ErrorOr<BookingPeriod> Create(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        List<Error> errors = [];
        var startResult = EnsureUtc(startAt, nameof(startAt));
        if (startResult.IsError)
        {
            errors.AddRange(startResult.Errors);
        }

        var endResult = EnsureUtc(endAt, nameof(endAt));
        if (endResult.IsError)
        {
            errors.AddRange(endResult.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        if (endResult.Value <= startResult.Value)
        {
            return Error.Validation("Booking.InvalidAppointmentPeriod", "Appointment end time must be after start time.");
        }

        return new BookingPeriod(startResult.Value, endResult.Value);
    }

    private static ErrorOr<DateTimeOffset> EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value == default)
        {
            return Error.Validation($"Booking.{parameterName}Required", $"{parameterName} is required.");
        }

        if (value.Offset != TimeSpan.Zero)
        {
            return Error.Validation($"Booking.{parameterName}MustBeUtc", $"{parameterName} must be UTC.");
        }

        return value.ToUniversalTime();
    }
}
