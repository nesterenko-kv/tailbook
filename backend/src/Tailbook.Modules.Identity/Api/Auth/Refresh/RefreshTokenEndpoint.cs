using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Api.Auth.BrowserSessions;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Identity.Api.Auth.Login;

namespace Tailbook.Modules.Identity.Api.Auth.Refresh;

public sealed class RefreshTokenEndpoint(
    IIdentitySessionService identitySessionService,
    BrowserSessionService browserSessionService) : Endpoint<RefreshTokenRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/identity/auth/refresh");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var refreshToken = browserSessionService.ResolveIdentityRefreshToken(HttpContext, req.RefreshToken);
        if (refreshToken.IsError)
        {
            await Send.ResultAsync(refreshToken.Errors.ToHttpResult());
            return;
        }

        var result = await identitySessionService.RefreshSessionAsync(refreshToken.Value.RefreshToken, requireClientPortalAccess: false, ct);
        if (result.IsError)
        {
            if (refreshToken.Value.Source == BrowserRefreshTokenSource.Cookie && refreshToken.Value.Surface is not null)
            {
                browserSessionService.ClearRefreshCookie(HttpContext, refreshToken.Value.Surface);
            }

            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var browserSession = browserSessionService.ApplyIdentitySession(HttpContext, result.Value);
        if (browserSession.IsError)
        {
            await Send.ResultAsync(browserSession.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(
            LoginResponseMapper.FromLoginResult(result.Value, browserSession.Value.IncludeRefreshTokenInResponse),
            cancellation: ct);
    }
}

public sealed class RefreshTokenRequestValidator : Validator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).MaximumLength(512);
    }
}

public sealed class RevokeRefreshTokenEndpoint(
    IIdentitySessionService identitySessionService,
    BrowserSessionService browserSessionService) : Endpoint<RefreshTokenRequest>
{
    public override void Configure()
    {
        Post("/api/identity/auth/revoke");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var refreshToken = browserSessionService.ResolveIdentityRefreshToken(HttpContext, req.RefreshToken);
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
