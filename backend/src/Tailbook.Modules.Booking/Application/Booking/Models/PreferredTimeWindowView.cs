namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record PreferredTimeWindowView(DateTimeOffset StartAt, DateTimeOffset EndAt, string? Label);