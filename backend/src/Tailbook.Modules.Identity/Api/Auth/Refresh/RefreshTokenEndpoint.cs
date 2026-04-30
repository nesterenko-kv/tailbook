using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Api.Auth.Login;

namespace Tailbook.Modules.Identity.Api.Auth.Refresh;

public sealed class RefreshTokenEndpoint(IdentitySessionService identitySessionService) : Endpoint<RefreshTokenRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/identity/auth/refresh");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var result = await identitySessionService.RefreshSessionAsync(req.RefreshToken, requireClientPortalAccess: false, ct);
        if (result is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(new LoginResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.ExpiresAtUtc,
            RefreshToken = result.RefreshToken,
            RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc,
            User = result.User
        }, cancellation: ct);
    }
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequestValidator : Validator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}

public sealed class RevokeRefreshTokenEndpoint(IdentitySessionService identitySessionService) : Endpoint<RefreshTokenRequest>
{
    public override void Configure()
    {
        Post("/api/identity/auth/revoke");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        await identitySessionService.RevokeRefreshTokenAsync(req.RefreshToken, ct);
        await Send.NoContentAsync(ct);
    }
}
