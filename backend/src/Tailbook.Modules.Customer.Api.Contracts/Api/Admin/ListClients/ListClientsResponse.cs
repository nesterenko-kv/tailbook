namespace Tailbook.Modules.Customer.Api.Admin.ListClients;

public sealed class ListClientsResponse
{
    public IReadOnlyCollection<ClientListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
