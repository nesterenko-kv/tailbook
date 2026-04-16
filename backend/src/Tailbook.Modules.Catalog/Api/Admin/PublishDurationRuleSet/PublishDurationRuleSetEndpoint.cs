using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Catalog.Application;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishDurationRuleSet;

public sealed class PublishDurationRuleSetEndpoint(ICurrentUser currentUser, ICatalogAccessPolicy accessPolicy, CatalogPricingQueries pricingQueries)
    : Endpoint<PublishDurationRuleSetRequest, PublishDurationRuleSetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/duration/rule-sets/{ruleSetId:guid}/publish");
        Description(x => x.WithTags("Admin Duration"));
    }

    public override async Task HandleAsync(PublishDurationRuleSetRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanWriteCatalog(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await pricingQueries.PublishDurationRuleSetAsync(req.RuleSetId, ct);
            if (result is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.ResponseAsync(PublishDurationRuleSetResponse.FromView(result), cancellation: ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
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
