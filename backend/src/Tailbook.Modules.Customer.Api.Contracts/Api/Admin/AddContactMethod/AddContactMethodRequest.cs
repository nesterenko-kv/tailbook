namespace Tailbook.Modules.Customer.Api.Admin.AddContactMethod;

public sealed class AddContactMethodRequest
{
    public Guid ContactId { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? DisplayValue { get; set; }
    public bool IsPreferred { get; set; }
    public string? VerificationStatus { get; set; }
    public string? Notes { get; set; }
}
