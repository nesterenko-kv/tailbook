using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Application;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishPriceRuleSet;

public sealed class PublishPriceRuleSetEndpoint(CatalogPricingQueries pricingQueries)
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
        try
        {
            var result = await pricingQueries.PublishPriceRuleSetAsync(req.RuleSetId, ct);
            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.ResponseAsync(PublishPriceRuleSetResponse.FromView(result), cancellation: ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
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
