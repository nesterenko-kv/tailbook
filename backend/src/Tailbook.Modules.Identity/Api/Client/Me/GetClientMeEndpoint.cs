using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Client.Me;

public sealed class GetClientMeEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService)
    : Endpoint<GetClientMeRequest, ClientMeResponse>
{
    public override void Configure()
    {
        Get("/api/client/me");
        Description(x => x.WithTags("Client Portal Identity"));
        PermissionsAll(PermissionCodes.ClientPortalAccess);
    }

    public override async Task HandleAsync(GetClientMeRequest req, CancellationToken ct)
    {
        var actorResult = await actorService.GetActorAsync(req.UserId, ct);
        if (actorResult.IsError)
        {
            await Send.ResultAsync(actorResult.Errors.ToHttpResult());
            return;
        }

        var actor = actorResult.Value;
        await Send.OkAsync(new ClientMeResponse
        {
            UserId = actor.UserId,
            ClientId = actor.ClientId,
            ContactPersonId = actor.ContactPersonId,
            Email = actor.Email,
            DisplayName = actor.DisplayName,
            Roles = currentUser.Roles,
            Permissions = currentUser.Permissions
        }, cancellation: ct);
    }
}
