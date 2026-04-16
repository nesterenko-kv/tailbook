using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Catalog.Application;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.GetOfferById;

public sealed class GetOfferByIdEndpoint(ICurrentUser currentUser, ICatalogAccessPolicy accessPolicy, CatalogQueries catalogQueries)
    : Endpoint<GetOfferByIdRequest, OfferResponse>
{
    public override void Configure()
    {
        Get("/api/admin/catalog/offers/{id:guid}");
        Description(x => x.WithTags("Admin Catalog"));
    }

    public override async Task HandleAsync(GetOfferByIdRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanReadCatalog(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var offer = await catalogQueries.GetOfferAsync(req.Id, ct);
        if (offer is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(OfferResponse.Map(offer), ct);
    }
}

public sealed class GetOfferByIdRequest
{
    public Guid Id { get; set; }
}
