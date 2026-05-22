using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Pets.Api.Admin.GetPetCatalog;

public sealed class GetPetCatalogEndpoint(IPetsReadService petsReadService)
    : EndpointWithoutRequest<GetPetCatalogResponse>
{
    public override void Configure()
    {
        Get("/api/admin/pets/catalog");
        Description(x => x.WithTags("Admin Pets"));
        Permissions("pets.catalog.read", "pets.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var catalog = await petsReadService.GetCatalogAsync(ct);
        await Send.OkAsync(PetCatalogResponseMapper.ToAdminPetCatalogResponse(catalog), ct);
    }
}
