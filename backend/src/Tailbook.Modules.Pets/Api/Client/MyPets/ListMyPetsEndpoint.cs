using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;

namespace Tailbook.Modules.Pets.Api.Client.MyPets;

public sealed class ListMyPetsEndpoint(IClientPortalActorService actorService, IClientPortalPetsReadService petsReadService)
    : Endpoint<ListMyPetsRequest, IReadOnlyCollection<ClientPetSummaryView>>
{
    public override void Configure()
    {
        Get("/api/client/me/pets");
        Description(x => x.WithTags("Client Portal Pets"));
        PermissionsAll(PermissionCodes.ClientPetsRead);
    }

    public override async Task HandleAsync(ListMyPetsRequest req, CancellationToken ct)
    {
        var actorResult = await actorService.GetActorAsync(req.UserId, ct);
        if (actorResult.IsError)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var actor = actorResult.Value;
        var result = await petsReadService.ListMyPetsAsync(actor.ClientId, ct);
        await Send.OkAsync(result, cancellation: ct);
    }
}
