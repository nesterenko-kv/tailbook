using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.ListPriceRuleSets;

public sealed class ListPriceRuleSetsEndpoint(CatalogPricingQueries pricingQueries)
    : EndpointWithoutRequest<ListPriceRuleSetsResponse>
{
    public override void Configure()
    {
        Get("/api/admin/pricing/rule-sets");
        Description(x => x.WithTags("Admin Pricing"));
        PermissionsAll("catalog.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
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
