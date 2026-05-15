namespace Tailbook.Modules.Customer.Api.Admin.AddContactMethod;

public sealed class AddContactMethodResponse
{
    public Guid Id { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
