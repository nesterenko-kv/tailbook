using ErrorOr;

namespace Tailbook.Modules.Booking.Application.Common.Validation;

public static class BookingTimeInputNormalizer
{
    // Keeps application-boundary inputs normalized while BookingPeriod stays strict UTC-only.
    public static ErrorOr<DateTimeOffset> AssumeUtc(DateTimeOffset value, string parameterName)
    {
        if (value == default)
        {
            return Error.Validation($"Booking.{parameterName}Required", $"{parameterName} is required.");
        }

        return value.ToUniversalTime();
    }

    public static ErrorOr<BookingPeriod> CreatePeriod(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        List<Error> errors = [];
        var normalizedStart = AssumeUtc(startAt, nameof(startAt));
        if (normalizedStart.IsError)
        {
            errors.AddRange(normalizedStart.Errors);
        }

        var normalizedEnd = AssumeUtc(endAt, nameof(endAt));
        if (normalizedEnd.IsError)
        {
            errors.AddRange(normalizedEnd.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        if (normalizedEnd.Value <= normalizedStart.Value)
        {
            return Error.Validation("Booking.InvalidAppointmentPeriod", "Appointment end time must be after start time.");
        }

        return BookingPeriod.Create(normalizedStart.Value, normalizedEnd.Value);
    }
}
