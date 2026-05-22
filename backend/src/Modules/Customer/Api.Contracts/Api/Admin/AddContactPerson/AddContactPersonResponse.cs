namespace Tailbook.Modules.Customer.Api.Admin.AddContactPerson;

public sealed class AddContactPersonResponse
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Notes { get; set; }
    public string TrustLevel { get; set; } = string.Empty;
}
