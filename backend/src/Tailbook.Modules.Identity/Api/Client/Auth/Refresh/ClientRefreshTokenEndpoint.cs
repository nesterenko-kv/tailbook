using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Api.Auth.BrowserSessions;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Identity.Api.Auth.Refresh;
using Tailbook.Modules.Identity.Api.Client.Auth.Login;

namespace Tailbook.Modules.Identity.Api.Client.Auth.Refresh;

public sealed class ClientRefreshTokenEndpoint(
    IIdentitySessionService identitySessionService,
    BrowserSessionService browserSessionService) : Endpoint<RefreshTokenRequest, ClientLoginResponse>
{
    public override void Configure()
    {
        Post("/api/client/auth/refresh");
        AllowAnonymous();
        Description(x => x.WithTags("Client Portal Identity"));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var refreshToken = browserSessionService.ResolveClientRefreshToken(HttpContext, req.RefreshToken);
        if (refreshToken.IsError)
        {
            await Send.ResultAsync(refreshToken.Errors.ToHttpResult());
            return;
        }

        var result = await identitySessionService.RefreshSessionAsync(refreshToken.Value.RefreshToken, requireClientPortalAccess: true, ct);
        if (result.IsError)
        {
            if (refreshToken.Value.Source == BrowserRefreshTokenSource.Cookie && refreshToken.Value.Surface is not null)
            {
                browserSessionService.ClearRefreshCookie(HttpContext, refreshToken.Value.Surface);
            }

            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var login = result.Value;
        var browserSession = browserSessionService.ApplyClientSession(HttpContext, login);
        if (browserSession.IsError)
        {
            await Send.ResultAsync(browserSession.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(new ClientLoginResponse
        {
            AccessToken = login.AccessToken,
            ExpiresAt = login.ExpiresAt,
            RefreshToken = browserSession.Value.IncludeRefreshTokenInResponse ? login.RefreshToken : null,
            RefreshTokenExpiresAt = login.RefreshTokenExpiresAt,
            User = login.User
        }, cancellation: ct);
    }
}

public sealed class ClientRevokeRefreshTokenEndpoint(
    IIdentitySessionService identitySessionService,
    BrowserSessionService browserSessionService) : Endpoint<RefreshTokenRequest>
{
    public override void Configure()
    {
        Post("/api/client/auth/revoke");
        AllowAnonymous();
        Description(x => x.WithTags("Client Portal Identity"));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var refreshToken = browserSessionService.ResolveClientRefreshToken(HttpContext, req.RefreshToken);
        if (refreshToken.IsError)
        {
            await Send.ResultAsync(refreshToken.Errors.ToHttpResult());
            return;
        }

        var result = await identitySessionService.RevokeRefreshTokenAsync(refreshToken.Value.RefreshToken, ct);
        if (refreshToken.Value.Source == BrowserRefreshTokenSource.Cookie && refreshToken.Value.Surface is not null)
        {
            browserSessionService.ClearRefreshCookie(HttpContext, refreshToken.Value.Surface);
        }

        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        await Send.NoContentAsync(ct);
    }
}
