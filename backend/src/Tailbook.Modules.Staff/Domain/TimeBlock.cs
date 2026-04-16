namespace Tailbook.Modules.Staff.Domain;

public sealed class TimeBlock
{
    public Guid Id { get; set; }
    public Guid GroomerId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
