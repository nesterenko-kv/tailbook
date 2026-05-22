namespace Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

public sealed class WorkingScheduleResponse
{
    public Guid Id { get; set; }
    public Guid GroomerId { get; set; }
    public int Weekday { get; set; }
    public string StartLocalTime { get; set; } = string.Empty;
    public string EndLocalTime { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}