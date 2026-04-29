using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Application;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.AddOfferVersionComponent;

public sealed class AddOfferVersionComponentEndpoint(CatalogQueries catalogQueries)
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
        try
        {
            var component = await catalogQueries.AddComponentAsync(req.VersionId, req.ProcedureId, req.ComponentRole, req.SequenceNo, req.DefaultExpected, ct);
            if (component is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.ResponseAsync(OfferVersionComponentResponse.Map(component), StatusCodes.Status201Created, ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
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
