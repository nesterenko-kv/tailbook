namespace Tailbook.Modules.Pets.Api.Admin.GetPetDetail;

public sealed class PetContactResponse
{
    public Guid ContactId { get; set; }
    public Guid ClientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool CanPickUp { get; set; }
    public bool CanPay { get; set; }
    public bool ReceivesNotifications { get; set; }
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
    public IReadOnlyCollection<ContactMethodResponse> Methods { get; set; } = [];
}