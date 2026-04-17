using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Me;

public sealed class GetCurrentUserEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService) : EndpointWithoutRequest<GetCurrentUserResponse>
{
    public override void Configure()
    {
        Get("/api/identity/me");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        Guid? parsedUserId = null;
        Guid? clientId = null;
        Guid? contactPersonId = null;

        if (Guid.TryParse(currentUser.UserId, out var userId))
        {
            parsedUserId = userId;
            var actor = await actorService.GetActorAsync(userId, ct);
            clientId = actor?.ClientId;
            contactPersonId = actor?.ContactPersonId;
        }

        await Send.OkAsync(new GetCurrentUserResponse
        {
            UserId = parsedUserId,
            SubjectId = currentUser.SubjectId ?? string.Empty,
            Email = currentUser.Email ?? string.Empty,
            DisplayName = currentUser.DisplayName ?? string.Empty,
            ClientId = clientId,
            ContactPersonId = contactPersonId,
            Roles = currentUser.Roles,
            Permissions = currentUser.Permissions
        }, cancellation: ct);
    }
}
