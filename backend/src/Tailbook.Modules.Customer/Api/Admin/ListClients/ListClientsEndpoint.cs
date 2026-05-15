using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Customer.Api.Admin.ListClients;

public sealed class ListClientsEndpoint(
    ICustomerReadService customerReadService,
    IScopeAuthorizationService scopeAuthorizationService)
    : Endpoint<ListClientsRequest, ListClientsResponse>
{
    public override void Configure()
    {
        Get("/api/admin/clients");
        Description(x => x.WithTags("Admin CRM"));
        PermissionsAll("crm.clients.read");
    }

    public override async Task HandleAsync(ListClientsRequest req, CancellationToken ct)
    {
        var result = await customerReadService.ListClientsAsync(req.Search, req.Page, req.PageSize, ct);
        var actorUserId = User.FindFirst(TailbookClaimTypes.UserId)?.Value;
        IReadOnlyCollection<ClientListItemView> filteredItems = result.Items;
        var totalCount = result.TotalCount;

        if (Guid.TryParse(actorUserId, out var userId))
        {
            var hasGlobal = await scopeAuthorizationService.HasGlobalScopeAsync(userId, ct);
            if (!hasGlobal)
            {
                filteredItems = await ScopeFilter.ApplyAsync(
                    result.Items,
                    userId,
                    EntityScopeResourceTypes.Client,
                    item => item.Id.ToString("D"),
                    scopeAuthorizationService,
                    ct);
                totalCount = filteredItems.Count;
            }
        }

        await Send.OkAsync(new ListClientsResponse
        {
            Items = filteredItems.Select(x => new ClientListItemResponse
            {
                Id = x.Id,
                DisplayName = x.DisplayName,
                Status = x.Status,
                ContactCount = x.ContactCount,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = totalCount
        }, ct);
    }
}
