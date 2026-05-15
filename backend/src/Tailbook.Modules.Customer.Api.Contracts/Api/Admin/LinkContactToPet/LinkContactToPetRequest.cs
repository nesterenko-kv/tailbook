namespace Tailbook.Modules.Customer.Api.Admin.LinkContactToPet;

public sealed class LinkContactToPetRequest
{
    public Guid PetId { get; set; }
    public Guid ContactId { get; set; }
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
    public bool IsPrimary { get; set; }
    public bool CanPickUp { get; set; }
    public bool CanPay { get; set; }
    public bool ReceivesNotifications { get; set; } = true;
}
