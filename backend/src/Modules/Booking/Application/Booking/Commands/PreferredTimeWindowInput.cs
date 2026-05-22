namespace Tailbook.Modules.Booking.Application.Booking.Commands;

public sealed record PreferredTimeWindowInput(DateTimeOffset StartAt, DateTimeOffset EndAt, string? Label);