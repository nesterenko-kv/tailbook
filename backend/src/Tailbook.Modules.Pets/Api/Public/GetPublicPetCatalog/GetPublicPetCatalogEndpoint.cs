using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Pets.Api;

namespace Tailbook.Modules.Pets.Api.Public.GetPublicPetCatalog;

public sealed class GetPublicPetCatalogEndpoint(IPetsReadService petsReadService)
    : EndpointWithoutRequest<GetPublicPetCatalogResponse>
{
    public override void Configure()
    {
        Get("/api/public/pets/catalog");
        AllowAnonymous();
        Description(x => x.WithTags("Public Booking"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var catalog = await petsReadService.GetCatalogAsync(ct);
        await Send.OkAsync(PetCatalogResponseMapper.ToPublicPetCatalogResponse(catalog), ct);
    }
}
