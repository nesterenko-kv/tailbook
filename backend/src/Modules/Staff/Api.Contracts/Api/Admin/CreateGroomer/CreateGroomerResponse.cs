namespace Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

public sealed class CreateGroomerResponse
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool Active { get; set; }
    public GroomerCapabilityResponse[] Capabilities { get; set; } = [];
    public WorkingScheduleResponse[] WorkingSchedules { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}