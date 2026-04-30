namespace Tailbook.Modules.Staff.Domain.Aggregates;

public sealed class Groomer
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
