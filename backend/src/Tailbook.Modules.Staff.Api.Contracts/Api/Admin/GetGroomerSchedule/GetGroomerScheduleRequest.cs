namespace Tailbook.Modules.Staff.Api.Admin.GetGroomerSchedule;

public sealed class GetGroomerScheduleRequest
{
    public Guid GroomerId { get; set; }
    public DateTimeOffset From { get; set; }
    public DateTimeOffset To { get; set; }
}