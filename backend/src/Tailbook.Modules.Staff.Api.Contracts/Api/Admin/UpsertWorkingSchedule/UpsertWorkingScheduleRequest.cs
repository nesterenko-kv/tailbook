namespace Tailbook.Modules.Staff.Api.Admin.UpsertWorkingSchedule;

public sealed class UpsertWorkingScheduleRequest
{
    public Guid GroomerId { get; set; }
    public int Weekday { get; set; }
    public string StartLocalTime { get; set; } = string.Empty;
    public string EndLocalTime { get; set; } = string.Empty;
}