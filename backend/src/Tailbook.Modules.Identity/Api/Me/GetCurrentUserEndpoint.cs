using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Me;

public sealed class GetCurrentUserEndpoint(ICurrentUser currentUser) : EndpointWithoutRequest<GetCurrentUserResponse>
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

        await Send.OkAsync(new GetCurrentUserResponse
        {
            SubjectId = currentUser.SubjectId ?? string.Empty,
            Roles = currentUser.Roles,
            Permissions = currentUser.Permissions
        }, cancellation: ct);
    }
}
