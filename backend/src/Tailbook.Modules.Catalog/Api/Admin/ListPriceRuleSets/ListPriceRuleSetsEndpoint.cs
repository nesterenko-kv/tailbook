using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Catalog.Application;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.ListPriceRuleSets;

public sealed class ListPriceRuleSetsEndpoint(ICurrentUser currentUser, ICatalogAccessPolicy accessPolicy, CatalogPricingQueries pricingQueries)
    : EndpointWithoutRequest<ListPriceRuleSetsResponse>
{
    public override void Configure()
    {
        Get("/api/admin/pricing/rule-sets");
        Description(x => x.WithTags("Admin Pricing"));
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

        var items = await pricingQueries.ListPriceRuleSetsAsync(ct);
        await Send.ResponseAsync(new ListPriceRuleSetsResponse
        {
            Items = items.Select(PriceRuleSetResponseBase.FromView).ToArray()
        }, cancellation: ct);
    }
}

public sealed class ListPriceRuleSetsResponse
{
    public PriceRuleSetResponseBase[] Items { get; set; } = [];
}
