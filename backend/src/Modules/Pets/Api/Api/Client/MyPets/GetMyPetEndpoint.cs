using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;

namespace Tailbook.Modules.Pets.Api.Client.MyPets;

public sealed class GetMyPetEndpoint(IClientPortalActorService actorService, IClientPortalPetsReadService petsReadService)
    : Endpoint<GetMyPetRequest, ClientPetDetailView>
{
    public override void Configure()
    {
        Get("/api/client/me/pets/{petId:guid}");
        Description(x => x.WithTags("Client Portal Pets"));
        PermissionsAll(PermissionCodes.ClientPetsRead);
    }

    public override async Task HandleAsync(GetMyPetRequest req, CancellationToken ct)
    {
        var actorResult = await actorService.GetActorAsync(req.UserId, ct);
        if (actorResult.IsError)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var actor = actorResult.Value;
        var result = await petsReadService.GetMyPetAsync(actor.ClientId, req.PetId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, cancellation: ct);
    }
}