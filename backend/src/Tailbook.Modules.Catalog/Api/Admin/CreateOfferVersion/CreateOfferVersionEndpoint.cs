using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateOfferVersion;

public sealed class CreateOfferVersionEndpoint : Endpoint<CreateOfferVersionRequest, OfferVersionResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/offers/{offerId:guid}/versions");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(CreateOfferVersionRequest req, CancellationToken ct)
    {
        if (req.ValidFrom.HasValue && req.ValidTo.HasValue && req.ValidTo.Value < req.ValidFrom.Value)
        {
            AddError("ValidTo must be greater than or equal to ValidFrom.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var command = new CreateCatalogOfferVersionCommand(req.OfferId, req.ValidFrom, req.ValidTo, req.PolicyText, req.ChangeNote);
        var version = await command.ExecuteAsync(ct);
        if (version is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.ResponseAsync(OfferVersionResponse.Map(version), StatusCodes.Status201Created, ct);
    }
}
