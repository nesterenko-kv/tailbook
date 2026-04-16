using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Catalog.Application;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.ListDurationRuleSets;

public sealed class ListDurationRuleSetsEndpoint(ICurrentUser currentUser, ICatalogAccessPolicy accessPolicy, CatalogPricingQueries pricingQueries)
    : EndpointWithoutRequest<ListDurationRuleSetsResponse>
{
    public override void Configure()
    {
        Get("/api/admin/duration/rule-sets");
        Description(x => x.WithTags("Admin Duration"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanReadCatalog(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var items = await pricingQueries.ListDurationRuleSetsAsync(ct);
        await Send.ResponseAsync(new ListDurationRuleSetsResponse
        {
            Items = items.Select(DurationRuleSetResponseBase.FromView).ToArray()
        }, cancellation: ct);
    }
}

public sealed class ListDurationRuleSetsResponse
{
    public DurationRuleSetResponseBase[] Items { get; set; } = [];
}
