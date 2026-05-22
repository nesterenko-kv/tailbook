namespace Tailbook.Modules.Customer.Api.Admin.AddContactPerson;

public sealed class AddContactPersonRequest
{
    public Guid ClientId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Notes { get; set; }
    public string? TrustLevel { get; set; }
}
