using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateDurationRule;

public sealed class CreateDurationRuleEndpoint : Endpoint<CreateDurationRuleRequest, CreateDurationRuleResponse>
{
    public override void Configure()
    {
        Post("/api/admin/duration/rule-sets/{ruleSetId:guid}/rules");
        Description(x => x.WithTags("Admin Duration"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreateDurationRuleRequest req, CancellationToken ct)
    {
        var result = await new CreateCatalogDurationRuleCommand(
            req.RuleSetId,
            req.OfferId,
            req.Priority,
            req.BaseMinutes,
            req.BufferBeforeMinutes,
            req.BufferAfterMinutes,
            new RuleConditionInput(req.AnimalTypeId, req.BreedId, req.BreedGroupId, req.CoatTypeId, req.SizeCategoryId))
            .ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(CreateDurationRuleResponse.FromView(result.Value), StatusCodes.Status201Created, ct);
    }
}
