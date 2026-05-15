namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record RescheduleAppointmentInput(Guid AppointmentId, Guid GroomerId, DateTimeOffset StartAt, int ExpectedVersionNo);