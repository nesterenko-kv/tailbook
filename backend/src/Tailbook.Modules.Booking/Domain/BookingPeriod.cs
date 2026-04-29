namespace Tailbook.Modules.Booking.Domain;

public sealed record BookingPeriod
{
    public BookingPeriod(DateTime startAtUtc, DateTime endAtUtc)
    {
        StartAtUtc = EnsureUtc(startAtUtc, nameof(startAtUtc));
        EndAtUtc = EnsureUtc(endAtUtc, nameof(endAtUtc));

        if (EndAtUtc <= StartAtUtc)
        {
            throw new InvalidOperationException("Appointment end time must be after start time.");
        }
    }

    public DateTime StartAtUtc { get; }
    public DateTime EndAtUtc { get; }

    private static DateTime EnsureUtc(DateTime value, string parameterName)
    {
        if (value == default)
        {
            throw new InvalidOperationException($"{parameterName} is required.");
        }

        if (value.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException($"{parameterName} must be UTC.");
        }

        return value;
    }
}
