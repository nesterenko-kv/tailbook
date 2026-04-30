using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Api.Admin.PricingContracts;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateDurationRuleSet;

public sealed class CreateDurationRuleSetEndpoint()
    : Endpoint<CreateDurationRuleSetRequest, CreateDurationRuleSetResponse>
{
    public override void Configure()
    {
        Post("/api/admin/duration/rule-sets");
        Description(x => x.WithTags("Admin Duration"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreateDurationRuleSetRequest req, CancellationToken ct)
    {
        var result = await new CreateCatalogDurationRuleSetCommand(req.ValidFromUtc, req.ValidToUtc).ExecuteAsync(ct);
        await Send.ResponseAsync(new CreateDurationRuleSetResponse
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

public sealed class CreateDurationRuleSetRequest
{
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
}

public sealed class CreateDurationRuleSetRequestValidator : Validator<CreateDurationRuleSetRequest>
{
    public CreateDurationRuleSetRequestValidator()
    {
        RuleFor(x => x).Must(x => x.ValidToUtc is null || x.ValidFromUtc is null || x.ValidToUtc >= x.ValidFromUtc)
            .WithMessage("ValidToUtc must be greater than or equal to ValidFromUtc.");
    }
}

public sealed class CreateDurationRuleSetResponse : DurationRuleSetResponseBase
{
    public new static CreateDurationRuleSetResponse FromView(DurationRuleSetView view)
    {
        var baseResponse = DurationRuleSetResponseBase.FromView(view);
        return new CreateDurationRuleSetResponse
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
