using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Application;
using Tailbook.Modules.Identity.Api.Auth.Refresh;
using Tailbook.Modules.Identity.Api.Client.Auth.Login;

namespace Tailbook.Modules.Identity.Api.Client.Auth.Refresh;

public sealed class ClientRefreshTokenEndpoint(IdentitySessionService identitySessionService) : Endpoint<RefreshTokenRequest, ClientLoginResponse>
{
    public override void Configure()
    {
        Post("/api/client/auth/refresh");
        AllowAnonymous();
        Description(x => x.WithTags("Client Portal Identity"));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var result = await identitySessionService.RefreshSessionAsync(req.RefreshToken, requireClientPortalAccess: true, ct);
        if (result is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(new ClientLoginResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.ExpiresAtUtc,
            RefreshToken = result.RefreshToken,
            RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc,
            User = result.User
        }, cancellation: ct);
    }
}

public sealed class ClientRevokeRefreshTokenEndpoint(IdentitySessionService identitySessionService) : Endpoint<RefreshTokenRequest>
{
    public override void Configure()
    {
        Post("/api/client/auth/revoke");
        AllowAnonymous();
        Description(x => x.WithTags("Client Portal Identity"));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        await identitySessionService.RevokeRefreshTokenAsync(req.RefreshToken, ct);
        await Send.NoContentAsync(ct);
    }
}
