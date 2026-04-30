using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateOfferVersion;

public sealed class CreateOfferVersionEndpoint(CatalogQueries catalogQueries)
    : Endpoint<CreateOfferVersionRequest, OfferVersionResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/offers/{offerId:guid}/versions");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreateOfferVersionRequest req, CancellationToken ct)
    {
        if (req.ValidFromUtc.HasValue && req.ValidToUtc.HasValue && req.ValidToUtc.Value < req.ValidFromUtc.Value)
        {
            AddError("ValidToUtc must be greater than or equal to ValidFromUtc.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var version = await catalogQueries.CreateOfferVersionAsync(req.OfferId, req.ValidFromUtc, req.ValidToUtc, req.PolicyText, req.ChangeNote, ct);
        if (version is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(OfferVersionResponse.Map(version), StatusCodes.Status201Created, ct);
    }
}

public sealed class CreateOfferVersionRequest
{
    public Guid OfferId { get; set; }
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public string? PolicyText { get; set; }
    public string? ChangeNote { get; set; }
}

public sealed class CreateOfferVersionRequestValidator : Validator<CreateOfferVersionRequest>
{
    public CreateOfferVersionRequestValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.PolicyText).MaximumLength(4000);
        RuleFor(x => x.ChangeNote).MaximumLength(1000);
    }
}
