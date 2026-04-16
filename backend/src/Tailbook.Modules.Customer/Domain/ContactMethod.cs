namespace Tailbook.Modules.Customer.Domain;

public sealed class ContactMethod
{
    public Guid Id { get; set; }
    public Guid ContactPersonId { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
