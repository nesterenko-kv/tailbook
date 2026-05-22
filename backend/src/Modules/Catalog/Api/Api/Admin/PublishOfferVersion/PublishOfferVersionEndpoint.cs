using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishOfferVersion;

public sealed class PublishOfferVersionEndpoint : Endpoint<PublishOfferVersionRequest, OfferVersionResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/offer-versions/{versionId:guid}/publish");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(PublishOfferVersionRequest req, CancellationToken ct)
    {
        var command = new PublishCatalogOfferVersionCommand(req.VersionId);
        var result = await command.ExecuteAsync(ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(OfferVersionResponse.Map(result.Value), ct);
    }
}
