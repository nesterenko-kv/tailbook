using ErrorOr;

namespace Tailbook.Modules.Booking.Application.Common.Validation;

public static class BookingTimeInputNormalizer
{
    // Keeps legacy HTTP DateTime inputs compatible while BookingPeriod stays strict UTC-only.
    public static DateTime AssumeUtc(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw new InvalidOperationException($"{parameterName} is required.");
        }

        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static ErrorOr<DateTime> TryAssumeUtc(DateTime value, string parameterName)
    {
        if (value == default)
        {
            return Error.Validation($"Booking.{parameterName}Required", $"{parameterName} is required.");
        }

        return value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public static BookingPeriod CreatePeriod(DateTime startAtUtc, DateTime endAtUtc)
    {
        return new BookingPeriod(
            AssumeUtc(startAtUtc, nameof(startAtUtc)),
            AssumeUtc(endAtUtc, nameof(endAtUtc)));
    }

    public static ErrorOr<BookingPeriod> TryCreatePeriod(DateTime startAtUtc, DateTime endAtUtc)
    {
        var normalizedStart = TryAssumeUtc(startAtUtc, nameof(startAtUtc));
        if (normalizedStart.IsError)
        {
            return normalizedStart.Errors;
        }

        var normalizedEnd = TryAssumeUtc(endAtUtc, nameof(endAtUtc));
        if (normalizedEnd.IsError)
        {
            return normalizedEnd.Errors;
        }

        if (normalizedEnd.Value <= normalizedStart.Value)
        {
            return Error.Validation("Booking.InvalidAppointmentPeriod", "Appointment end time must be after start time.");
        }

        return new BookingPeriod(normalizedStart.Value, normalizedEnd.Value);
    }
}
