namespace Tailbook.Modules.Customer.Api.Admin.CreateClient;

public sealed class CreateClientRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
