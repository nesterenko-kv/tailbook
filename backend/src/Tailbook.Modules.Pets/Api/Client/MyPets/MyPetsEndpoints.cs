using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Pets.Api.Client.MyPets;

public sealed class ListMyPetsEndpoint(IClientPortalActorService actorService, ClientPortalPetsQueries queries)
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
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await queries.ListMyPetsAsync(actor.ClientId, ct);
        await Send.OkAsync(result, cancellation: ct);
    }
}

public sealed class GetMyPetEndpoint(IClientPortalActorService actorService, ClientPortalPetsQueries queries)
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
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await queries.GetMyPetAsync(actor.ClientId, req.PetId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, cancellation: ct);
    }
}

public sealed class ListMyPetsRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }
}

public sealed class GetMyPetRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid PetId { get; set; }
}
