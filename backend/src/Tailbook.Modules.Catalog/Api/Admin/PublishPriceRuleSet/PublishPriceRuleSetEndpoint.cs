using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishPriceRuleSet;

public sealed class PublishPriceRuleSetEndpoint(ICatalogPricingQueries pricingQueries)
    : Endpoint<PublishPriceRuleSetRequest, PublishPriceRuleSetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/pricing/rule-sets/{ruleSetId:guid}/publish");
        Description(x => x.WithTags("Admin Pricing"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(PublishPriceRuleSetRequest req, CancellationToken ct)
    {
        var result = await pricingQueries.PublishPriceRuleSetAsync(req.RuleSetId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(PublishPriceRuleSetResponse.FromView(result.Value), cancellation: ct);
    }
}

public sealed class PublishPriceRuleSetRequest
{
    public Guid RuleSetId { get; set; }
}

public sealed class PublishPriceRuleSetResponse : PriceRuleSetResponseBase
{
    public new static PublishPriceRuleSetResponse FromView(PriceRuleSetView view)
    {
        var baseResponse = PriceRuleSetResponseBase.FromView(view);
        return new PublishPriceRuleSetResponse
        {
            Id = baseResponse.Id,
            VersionNo = baseResponse.VersionNo,
            Status = baseResponse.Status,
            ValidFromUtc = baseResponse.ValidFromUtc,
            ValidToUtc = baseResponse.ValidToUtc,
            CreatedAtUtc = baseResponse.CreatedAtUtc,
            PublishedAtUtc = baseResponse.PublishedAtUtc,
            Rules = baseResponse.Rules
        };
    }
}
