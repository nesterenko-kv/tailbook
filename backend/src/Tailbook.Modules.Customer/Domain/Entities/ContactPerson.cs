namespace Tailbook.Modules.Customer.Domain.Entities;

public sealed class ContactPerson
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Notes { get; set; }
    public string TrustLevel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
