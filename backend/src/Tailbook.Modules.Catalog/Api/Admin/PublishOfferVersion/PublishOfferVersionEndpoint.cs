using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Catalog.Application;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishOfferVersion;

public sealed class PublishOfferVersionEndpoint(CatalogQueries catalogQueries)
    : Endpoint<PublishOfferVersionRequest, OfferVersionResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/offer-versions/{versionId:guid}/publish");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.write");
    }

    public override async Task HandleAsync(PublishOfferVersionRequest req, CancellationToken ct)
    {
        var result = await catalogQueries.PublishOfferVersionAsync(req.VersionId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(OfferVersionResponse.Map(result.Value), ct);
    }
}

public sealed class PublishOfferVersionRequest
{
    public Guid VersionId { get; set; }
}
