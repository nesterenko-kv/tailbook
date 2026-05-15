namespace Tailbook.Modules.Staff.Application.Staff.Models;

public sealed record WorkingScheduleView(Guid Id, Guid GroomerId, int Weekday, string StartLocalTime, string EndLocalTime, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);