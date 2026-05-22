namespace Tailbook.Modules.Customer.Api.Admin.ListPetContactLinks;

public sealed class PetContactLinkResponse
{
    public Guid PetId { get; set; }
    public Guid ContactId { get; set; }
    public Guid ClientId { get; set; }
    public string ContactDisplayName { get; set; } = string.Empty;
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
    public bool IsPrimary { get; set; }
    public bool CanPickUp { get; set; }
    public bool CanPay { get; set; }
    public bool ReceivesNotifications { get; set; }
    public IReadOnlyCollection<ContactMethodResponse> Methods { get; set; } = [];
}
