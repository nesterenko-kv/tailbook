namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record ConvertBookingRequestToAppointmentInput(Guid BookingRequestId, Guid GroomerId, DateTimeOffset StartAt);