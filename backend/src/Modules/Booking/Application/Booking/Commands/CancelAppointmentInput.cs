namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record CancelAppointmentInput(Guid AppointmentId, int ExpectedVersionNo, string ReasonCode, string? Notes);