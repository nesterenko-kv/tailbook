using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishDurationRuleSet;

public sealed class PublishDurationRuleSetEndpoint : Endpoint<PublishDurationRuleSetRequest, PublishDurationRuleSetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/duration/rule-sets/{ruleSetId:guid}/publish");
        Description(x => x.WithTags("Admin Duration"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(PublishDurationRuleSetRequest req, CancellationToken ct)
    {
        var command = new PublishCatalogDurationRuleSetCommand(req.RuleSetId);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(PublishDurationRuleSetResponse.FromView(result.Value), cancellation: ct);
    }
}
