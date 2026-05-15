namespace Tailbook.Modules.Staff.Api.Admin.AddTimeBlock;

public sealed class AddTimeBlockRequest
{
    public Guid GroomerId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
}