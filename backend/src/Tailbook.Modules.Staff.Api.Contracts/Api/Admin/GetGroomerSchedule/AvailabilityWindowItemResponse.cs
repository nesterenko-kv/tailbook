namespace Tailbook.Modules.Staff.Api.Admin.GetGroomerSchedule;

public sealed class AvailabilityWindowItemResponse
{
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
}