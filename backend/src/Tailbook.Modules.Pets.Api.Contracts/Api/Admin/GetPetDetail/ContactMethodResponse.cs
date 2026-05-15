namespace Tailbook.Modules.Pets.Api.Admin.GetPetDetail;

public sealed class ContactMethodResponse
{
    public Guid Id { get; set; }
    public string MethodType { get; set; } = string.Empty;
    public string DisplayValue { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
}