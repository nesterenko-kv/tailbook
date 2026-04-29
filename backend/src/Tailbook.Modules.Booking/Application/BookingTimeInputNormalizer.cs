using Tailbook.Modules.Booking.Domain;

namespace Tailbook.Modules.Booking.Application;

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

    public static BookingPeriod CreatePeriod(DateTime startAtUtc, DateTime endAtUtc)
    {
        return new BookingPeriod(
            AssumeUtc(startAtUtc, nameof(startAtUtc)),
            AssumeUtc(endAtUtc, nameof(endAtUtc)));
    }
}
