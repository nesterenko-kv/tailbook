using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRuleSet;

public sealed class CreatePriceRuleSetEndpoint(ICatalogPricingQueries pricingQueries)
    : Endpoint<CreatePriceRuleSetRequest, CreatePriceRuleSetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/pricing/rule-sets");
        Description(x => x.WithTags("Admin Pricing"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreatePriceRuleSetRequest req, CancellationToken ct)
    {
        var result = await pricingQueries.CreatePriceRuleSetAsync(req.ValidFromUtc, req.ValidToUtc, ct);
        await Send.ResponseAsync(new CreatePriceRuleSetResponse
        {
            Id = result.Id,
            VersionNo = result.VersionNo,
            Status = result.Status,
            ValidFromUtc = result.ValidFromUtc,
            ValidToUtc = result.ValidToUtc,
            CreatedAtUtc = result.CreatedAtUtc,
            PublishedAtUtc = result.PublishedAtUtc,
            Rules = []
        }, StatusCodes.Status201Created, ct);
    }
}

public sealed class CreatePriceRuleSetRequest
{
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
}

public sealed class CreatePriceRuleSetRequestValidator : Validator<CreatePriceRuleSetRequest>
{
    public CreatePriceRuleSetRequestValidator()
    {
        RuleFor(x => x).Must(x => x.ValidToUtc is null || x.ValidFromUtc is null || x.ValidToUtc >= x.ValidFromUtc)
            .WithMessage("ValidToUtc must be greater than or equal to ValidFromUtc.");
    }
}

public sealed class CreatePriceRuleSetResponse : PriceRuleSetResponseBase
{
    public new static CreatePriceRuleSetResponse FromView(PriceRuleSetView view)
    {
        var baseResponse = PriceRuleSetResponseBase.FromView(view);
        return new CreatePriceRuleSetResponse
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
