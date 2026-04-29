using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Me;

public sealed class GetCurrentUserRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? UserId { get; set; }
}

public sealed class GetCurrentUserEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService) : Endpoint<GetCurrentUserRequest, GetCurrentUserResponse>
{
    public override void Configure()
    {
        Get("/api/identity/me");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(GetCurrentUserRequest req, CancellationToken ct)
    {
        Guid? clientId = null;
        Guid? contactPersonId = null;

        if (req.UserId.HasValue)
        {
            var actor = await actorService.GetActorAsync(req.UserId.Value, ct);
            clientId = actor?.ClientId;
            contactPersonId = actor?.ContactPersonId;
        }

        await Send.OkAsync(new GetCurrentUserResponse
        {
            UserId = req.UserId,
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
