namespace Tailbook.Modules.Customer.Domain.Aggregates;

public sealed class Client
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
