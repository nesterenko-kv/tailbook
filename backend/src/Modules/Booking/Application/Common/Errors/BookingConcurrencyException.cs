namespace Tailbook.Modules.Booking.Application.Common.Errors;

public sealed class BookingConcurrencyException(string message) : Exception(message);
