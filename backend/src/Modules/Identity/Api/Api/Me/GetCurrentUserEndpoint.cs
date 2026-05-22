using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Me;

public sealed class GetCurrentUserEndpoint(ICurrentUser currentUser) : Endpoint<GetCurrentUserRequest, GetCurrentUserResponse>
{
    public override void Configure()
    {
        Get("/api/identity/me");
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(GetCurrentUserRequest req, CancellationToken ct)
    {
        await Send.OkAsync(new GetCurrentUserResponse
        {
            UserId = req.UserId,
            SubjectId = currentUser.SubjectId ?? string.Empty,
            Email = currentUser.Email ?? string.Empty,
            DisplayName = currentUser.DisplayName ?? string.Empty,
            ClientId = req.ClientId,
            ContactPersonId = req.ContactPersonId,
            Roles = currentUser.Roles,
            Permissions = currentUser.Permissions
        }, cancellation: ct);
    }
}
