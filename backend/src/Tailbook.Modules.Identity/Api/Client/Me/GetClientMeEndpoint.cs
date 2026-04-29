using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Contracts;

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
        var actor = await actorService.GetActorAsync(req.UserId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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

public sealed class GetClientMeRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }
}

public sealed class ClientMeResponse
{
    public Guid UserId { get; set; }
    public Guid ClientId { get; set; }
    public Guid ContactPersonId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}
