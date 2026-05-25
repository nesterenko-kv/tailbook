namespace Tailbook.Modules.Customer.Api.Admin.UpdateClient;

public sealed class UpdateClientRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
