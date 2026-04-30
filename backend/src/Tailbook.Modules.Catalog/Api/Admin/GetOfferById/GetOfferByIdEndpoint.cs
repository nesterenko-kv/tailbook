using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.GetOfferById;

public sealed class GetOfferByIdEndpoint(ICatalogReadService catalogReadService)
    : Endpoint<GetOfferByIdRequest, OfferResponse>
{
    public override void Configure()
    {
        Get("/api/admin/catalog/offers/{id:guid}");
        Description(x => x.WithTags("Admin Catalog"));
        PermissionsAll("catalog.read");
    }

    public override async Task HandleAsync(GetOfferByIdRequest req, CancellationToken ct)
    {
        var offer = await catalogReadService.GetOfferAsync(req.Id, ct);
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
