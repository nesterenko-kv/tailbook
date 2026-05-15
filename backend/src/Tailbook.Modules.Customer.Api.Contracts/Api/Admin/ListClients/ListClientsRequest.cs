namespace Tailbook.Modules.Customer.Api.Admin.ListClients;

public sealed class ListClientsRequest
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
