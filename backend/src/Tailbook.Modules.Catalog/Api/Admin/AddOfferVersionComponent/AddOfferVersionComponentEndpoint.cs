using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.AddOfferVersionComponent;

public sealed class AddOfferVersionComponentEndpoint : Endpoint<AddOfferVersionComponentRequest, OfferVersionComponentResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/offer-versions/{versionId:guid}/components");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(AddOfferVersionComponentRequest req, CancellationToken ct)
    {
        var command = new AddCatalogOfferVersionComponentCommand(req.VersionId, req.ProcedureId, req.ComponentRole, req.SequenceNo, req.DefaultExpected);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.ResponseAsync(OfferVersionComponentResponse.Map(result.Value), StatusCodes.Status201Created, ct);
    }
}
