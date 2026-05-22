namespace Tailbook.Modules.Staff.Domain.Entities;

public sealed class WorkingSchedule
{
    public Guid Id { get; set; }
    public Guid GroomerId { get; set; }
    public int Weekday { get; set; }
    public TimeSpan StartLocalTime { get; set; }
    public TimeSpan EndLocalTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
