using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRule;

public sealed class CreatePriceRuleEndpoint(CatalogPricingQueries pricingQueries)
    : Endpoint<CreatePriceRuleRequest, CreatePriceRuleResponse>
{
    public override void Configure()
    {
        Post("/api/admin/pricing/rule-sets/{ruleSetId:guid}/rules");
        Description(x => x.WithTags("Admin Pricing"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreatePriceRuleRequest req, CancellationToken ct)
    {
        var result = await pricingQueries.CreatePriceRuleAsync(
            new CreatePriceRuleCommand(
                req.RuleSetId,
                req.OfferId,
                req.Priority,
                req.FixedAmount,
                req.Currency,
                new RuleConditionInput(req.AnimalTypeId, req.BreedId, req.BreedGroupId, req.CoatTypeId, req.SizeCategoryId)),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(CreatePriceRuleResponse.FromView(result.Value), StatusCodes.Status201Created, ct);
    }
}

public sealed class CreatePriceRuleRequest
{
    public Guid RuleSetId { get; set; }
    public Guid OfferId { get; set; }
    public int Priority { get; set; } = 100;
    public decimal FixedAmount { get; set; }
    public string Currency { get; set; } = "UAH";
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
}

public sealed class CreatePriceRuleRequestValidator : Validator<CreatePriceRuleRequest>
{
    public CreatePriceRuleRequestValidator()
    {
        RuleFor(x => x.RuleSetId).NotEmpty();
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FixedAmount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(8);
    }
}

public sealed class CreatePriceRuleResponse : PriceRuleResponseBase
{
    public new static CreatePriceRuleResponse FromView(PriceRuleView view)
    {
        var baseResponse = PriceRuleResponseBase.FromView(view);
        return new CreatePriceRuleResponse
        {
            Id = baseResponse.Id,
            RuleSetId = baseResponse.RuleSetId,
            OfferId = baseResponse.OfferId,
            OfferCode = baseResponse.OfferCode,
            OfferDisplayName = baseResponse.OfferDisplayName,
            Priority = baseResponse.Priority,
            SpecificityScore = baseResponse.SpecificityScore,
            ActionType = baseResponse.ActionType,
            FixedAmount = baseResponse.FixedAmount,
            Currency = baseResponse.Currency,
            Condition = baseResponse.Condition,
            CreatedAtUtc = baseResponse.CreatedAtUtc
        };
    }
}
