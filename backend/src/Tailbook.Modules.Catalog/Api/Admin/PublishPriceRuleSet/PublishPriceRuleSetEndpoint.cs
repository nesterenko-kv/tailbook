using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishPriceRuleSet;

public sealed class PublishPriceRuleSetEndpoint : Endpoint<PublishPriceRuleSetRequest, PublishPriceRuleSetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/pricing/rule-sets/{ruleSetId:guid}/publish");
        Description(x => x.WithTags("Admin Pricing"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(PublishPriceRuleSetRequest req, CancellationToken ct)
    {
        var command = new PublishCatalogPriceRuleSetCommand(req.RuleSetId);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(PublishPriceRuleSetResponse.FromView(result.Value), cancellation: ct);
    }
}
