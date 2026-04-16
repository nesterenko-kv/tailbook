namespace Tailbook.Modules.Booking.Application;

public sealed class BookingConcurrencyException(string message) : Exception(message);
