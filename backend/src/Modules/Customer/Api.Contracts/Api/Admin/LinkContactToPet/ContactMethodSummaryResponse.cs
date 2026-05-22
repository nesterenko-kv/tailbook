namespace Tailbook.Modules.Customer.Api.Admin.LinkContactToPet;

public sealed class ContactMethodSummaryResponse
{
    public Guid Id { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
}
