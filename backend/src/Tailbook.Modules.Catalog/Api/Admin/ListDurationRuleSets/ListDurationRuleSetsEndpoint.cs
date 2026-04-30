using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.ListDurationRuleSets;

public sealed class ListDurationRuleSetsEndpoint(CatalogPricingQueries pricingQueries)
    : EndpointWithoutRequest<ListDurationRuleSetsResponse>
{
    public override void Configure()
    {
        Get("/api/admin/duration/rule-sets");
        Description(x => x.WithTags("Admin Duration"));
        PermissionsAll("catalog.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
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
