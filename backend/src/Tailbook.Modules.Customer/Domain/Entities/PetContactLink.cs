namespace Tailbook.Modules.Customer.Domain.Entities;

public sealed class PetContactLink
{
    public Guid Id { get; set; }
    public Guid PetId { get; set; }
    public Guid ContactPersonId { get; set; }
    public string RoleCodes { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool CanPickUp { get; set; }
    public bool CanPay { get; set; }
    public bool ReceivesNotifications { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
