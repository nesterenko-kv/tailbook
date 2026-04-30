using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.AddOfferVersionComponent;

public sealed class AddOfferVersionComponentEndpoint(ICatalogQueries catalogQueries)
    : Endpoint<AddOfferVersionComponentRequest, OfferVersionComponentResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/offer-versions/{versionId:guid}/components");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(AddOfferVersionComponentRequest req, CancellationToken ct)
    {
        var result = await catalogQueries.AddComponentAsync(req.VersionId, req.ProcedureId, req.ComponentRole, req.SequenceNo, req.DefaultExpected, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(OfferVersionComponentResponse.Map(result.Value), StatusCodes.Status201Created, ct);
    }
}

public sealed class AddOfferVersionComponentRequest
{
    public Guid VersionId { get; set; }
    public Guid ProcedureId { get; set; }
    public string ComponentRole { get; set; } = string.Empty;
    public int SequenceNo { get; set; }
    public bool DefaultExpected { get; set; } = true;
}

public sealed class AddOfferVersionComponentRequestValidator : Validator<AddOfferVersionComponentRequest>
{
    public AddOfferVersionComponentRequestValidator()
    {
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.ProcedureId).NotEmpty();
        RuleFor(x => x.ComponentRole).NotEmpty().MaximumLength(32);
        RuleFor(x => x.SequenceNo).GreaterThan(0);
    }
}
