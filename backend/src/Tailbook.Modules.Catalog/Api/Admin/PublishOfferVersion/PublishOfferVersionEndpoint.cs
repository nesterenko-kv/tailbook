using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Catalog.Application;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.PublishOfferVersion;

public sealed class PublishOfferVersionEndpoint(ICurrentUser currentUser, ICatalogAccessPolicy accessPolicy, CatalogQueries catalogQueries)
    : Endpoint<PublishOfferVersionRequest, OfferVersionResponse>
{
    public override void Configure()
    {
        Post("/api/admin/catalog/offer-versions/{versionId:guid}/publish");
        Description(x => x.WithTags("Admin Catalog"));
    }

    public override async Task HandleAsync(PublishOfferVersionRequest req, CancellationToken ct)
    {
        if (!accessPolicy.CanWriteCatalog(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var version = await catalogQueries.PublishOfferVersionAsync(req.VersionId, ct);
            if (version is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(OfferVersionResponse.Map(version), ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class PublishOfferVersionRequest
{
    public Guid VersionId { get; set; }
}
