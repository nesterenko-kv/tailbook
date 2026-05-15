namespace Tailbook.Modules.Staff.Domain.Entities;

public sealed class TimeBlock
{
    public Guid Id { get; set; }
    public Guid GroomerId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
