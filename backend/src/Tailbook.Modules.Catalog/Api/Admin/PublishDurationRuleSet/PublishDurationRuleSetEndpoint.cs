using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishDurationRuleSet;

public sealed class PublishDurationRuleSetEndpoint()
    : Endpoint<PublishDurationRuleSetRequest, PublishDurationRuleSetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/duration/rule-sets/{ruleSetId:guid}/publish");
        Description(x => x.WithTags("Admin Duration"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(PublishDurationRuleSetRequest req, CancellationToken ct)
    {
        var result = await new PublishCatalogDurationRuleSetCommand(req.RuleSetId).ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(PublishDurationRuleSetResponse.FromView(result.Value), cancellation: ct);
    }
}

public sealed class PublishDurationRuleSetRequest
{
    public Guid RuleSetId { get; set; }
}

public sealed class PublishDurationRuleSetResponse : DurationRuleSetResponseBase
{
    public new static PublishDurationRuleSetResponse FromView(DurationRuleSetView view)
    {
        var baseResponse = DurationRuleSetResponseBase.FromView(view);
        return new PublishDurationRuleSetResponse
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
