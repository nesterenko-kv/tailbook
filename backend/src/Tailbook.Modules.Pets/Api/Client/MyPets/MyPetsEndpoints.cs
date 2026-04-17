using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Contracts;
using Tailbook.Modules.Pets.Application;

namespace Tailbook.Modules.Pets.Api.Client.MyPets;

public sealed class ListMyPetsEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService, ClientPortalPetsQueries queries)
    : EndpointWithoutRequest<IReadOnlyCollection<ClientPetSummaryView>>
{
    public override void Configure()
    {
        Get("/api/client/me/pets");
        Description(x => x.WithTags("Client Portal Pets"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || !currentUser.HasPermission(PermissionCodes.ClientPetsRead) || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var actor = await actorService.GetActorAsync(userId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var result = await queries.ListMyPetsAsync(actor.ClientId, ct);
        await Send.OkAsync(result, cancellation: ct);
    }
}

public sealed class GetMyPetEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService, ClientPortalPetsQueries queries)
    : EndpointWithoutRequest<ClientPetDetailView>
{
    public override void Configure()
    {
        Get("/api/client/me/pets/{petId:guid}");
        Description(x => x.WithTags("Client Portal Pets"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || !currentUser.HasPermission(PermissionCodes.ClientPetsRead) || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!Route<Guid?>("petId").HasValue)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var actor = await actorService.GetActorAsync(userId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var petId = Route<Guid>("petId");
        var result = await queries.GetMyPetAsync(actor.ClientId, petId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, cancellation: ct);
    }
}
