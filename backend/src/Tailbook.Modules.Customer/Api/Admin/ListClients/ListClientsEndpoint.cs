using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Customer.Application;

namespace Tailbook.Modules.Customer.Api.Admin.ListClients;

public sealed class ListClientsEndpoint(ICurrentUser currentUser, ICustomerAccessPolicy accessPolicy, CustomerQueries customerQueries)
    : Endpoint<ListClientsRequest, ListClientsResponse>
{
    public override void Configure()
    {
        Get("/api/admin/clients");
        Description(x => x.WithTags("Admin CRM"));
    }

    public override async Task HandleAsync(ListClientsRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanReadClients(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var result = await customerQueries.ListClientsAsync(req.Search, req.Page, req.PageSize, ct);
        await Send.OkAsync(new ListClientsResponse
        {
            Items = result.Items.Select(x => new ClientListItemResponse
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Status = x.Status,
                ContactCount = x.ContactCount,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            }).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        }, ct);
    }
}

public sealed class ListClientsRequest
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ListClientsResponse
{
    public IReadOnlyCollection<ClientListItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class ClientListItemResponse
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ContactCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
