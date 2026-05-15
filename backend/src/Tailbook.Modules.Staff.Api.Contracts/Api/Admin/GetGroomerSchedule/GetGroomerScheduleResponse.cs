namespace Tailbook.Modules.Staff.Api.Admin.GetGroomerSchedule;

public sealed class GetGroomerScheduleResponse
{
    public Guid GroomerId { get; set; }
    public string GroomerDisplayName { get; set; } = string.Empty;
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
    public WorkingScheduleItemResponse[] WorkingSchedules { get; set; } = [];
    public TimeBlockItemResponse[] TimeBlocks { get; set; } = [];
    public AvailabilityWindowItemResponse[] AvailabilityWindows { get; set; } = [];
}