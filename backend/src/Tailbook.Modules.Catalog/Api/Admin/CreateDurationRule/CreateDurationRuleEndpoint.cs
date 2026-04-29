using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Catalog.Application;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateDurationRule;

public sealed class CreateDurationRuleEndpoint(CatalogPricingQueries pricingQueries)
    : Endpoint<CreateDurationRuleRequest, CreateDurationRuleResponse>
{
    public override void Configure()
    {
        Post("/api/admin/duration/rule-sets/{ruleSetId:guid}/rules");
        Description(x => x.WithTags("Admin Duration"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreateDurationRuleRequest req, CancellationToken ct)
    {
        var result = await pricingQueries.CreateDurationRuleAsync(
            new CreateDurationRuleCommand(
                req.RuleSetId,
                req.OfferId,
                req.Priority,
                req.BaseMinutes,
                req.BufferBeforeMinutes,
                req.BufferAfterMinutes,
                new RuleConditionInput(req.AnimalTypeId, req.BreedId, req.BreedGroupId, req.CoatTypeId, req.SizeCategoryId)),
            ct);

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(CreateDurationRuleResponse.FromView(result.Value), StatusCodes.Status201Created, ct);
    }
}

public sealed class CreateDurationRuleRequest
{
    public Guid RuleSetId { get; set; }
    public Guid OfferId { get; set; }
    public int Priority { get; set; } = 100;
    public int BaseMinutes { get; set; }
    public int BufferBeforeMinutes { get; set; }
    public int BufferAfterMinutes { get; set; }
    public Guid? AnimalTypeId { get; set; }
    public Guid? BreedId { get; set; }
    public Guid? BreedGroupId { get; set; }
    public Guid? CoatTypeId { get; set; }
    public Guid? SizeCategoryId { get; set; }
}

public sealed class CreateDurationRuleRequestValidator : Validator<CreateDurationRuleRequest>
{
    public CreateDurationRuleRequestValidator()
    {
        RuleFor(x => x.RuleSetId).NotEmpty();
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BaseMinutes).GreaterThan(0);
        RuleFor(x => x.BufferBeforeMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BufferAfterMinutes).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateDurationRuleResponse : DurationRuleResponseBase
{
    public new static CreateDurationRuleResponse FromView(DurationRuleView view)
    {
        var baseResponse = DurationRuleResponseBase.FromView(view);
        return new CreateDurationRuleResponse
        {
            Id = baseResponse.Id,
            RuleSetId = baseResponse.RuleSetId,
            OfferId = baseResponse.OfferId,
            OfferCode = baseResponse.OfferCode,
            OfferDisplayName = baseResponse.OfferDisplayName,
            Priority = baseResponse.Priority,
            SpecificityScore = baseResponse.SpecificityScore,
            BaseMinutes = baseResponse.BaseMinutes,
            BufferBeforeMinutes = baseResponse.BufferBeforeMinutes,
            BufferAfterMinutes = baseResponse.BufferAfterMinutes,
            Condition = baseResponse.Condition,
            CreatedAtUtc = baseResponse.CreatedAtUtc
        };
    }
}
