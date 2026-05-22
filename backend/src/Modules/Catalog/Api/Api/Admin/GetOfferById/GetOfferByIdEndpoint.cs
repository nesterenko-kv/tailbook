using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Catalog.Api.Admin.CreateOffer;

namespace Tailbook.Modules.Catalog.Api.Admin.GetOfferById;

public sealed class GetOfferByIdEndpoint(
    ICatalogReadService catalogReadService,
    IEntityScopeService entityScopeService)
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

        var scopeResult = await entityScopeService.VerifyAccessAsync(EntityScopeResourceTypes.Offer, req.Id.ToString("D"), req.ActorUserId, ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(OfferResponse.Map(offer), ct);
    }
}