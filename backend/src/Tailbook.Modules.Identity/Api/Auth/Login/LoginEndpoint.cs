using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Auth.Login;

public sealed class LoginEndpoint(IdentityQueries identityQueries) : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/identity/auth/login");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await identityQueries.AuthenticateAsync(req.Email, req.Password, ct);
        if (result is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await HttpContext.Response.CompleteAsync();
            return;
        }

        await Send.OkAsync(new LoginResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.ExpiresAtUtc,
            User = result.User
        }, cancellation: ct);
    }
}
