using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRule;

public sealed class CreatePriceRuleEndpoint : Endpoint<CreatePriceRuleRequest, CreatePriceRuleResponse>
{
    public override void Configure()
    {
        Post("/api/admin/pricing/rule-sets/{ruleSetId:guid}/rules");
        Description(x => x.WithTags("Admin Pricing"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreatePriceRuleRequest req, CancellationToken ct)
    {
        var result = await new CreateCatalogPriceRuleCommand(
            new CreatePriceRuleContextData(
                req.RuleSetId,
                req.OfferId,
                req.Priority,
                req.FixedAmount,
                req.Currency,
                new RuleConditionInput(req.AnimalTypeId, req.BreedId, req.BreedGroupId, req.CoatTypeId, req.SizeCategoryId)
                )
            )
            .ExecuteAsync(ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(CreatePriceRuleResponse.FromView(result.Value), StatusCodes.Status201Created, ct);
    }
}
