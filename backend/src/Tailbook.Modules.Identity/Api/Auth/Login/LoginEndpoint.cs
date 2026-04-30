using System.Globalization;
using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Identity.Api.Auth.Login;

public sealed class LoginEndpoint(
    LoginThrottlingService loginThrottling,
    AuthenticateUserCommandHandler authenticateUserHandler) : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/identity/auth/login");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var throttleDecision = loginThrottling.CheckAllowed(req.Email);
        if (throttleDecision.IsLockedOut)
        {
            SetRetryAfterHeader(throttleDecision);
            await Send.ResultAsync(Results.Problem(
                title: "Too many login attempts",
                detail: "Too many failed login attempts. Try again later.",
                statusCode: StatusCodes.Status429TooManyRequests));
            return;
        }

        var result = await authenticateUserHandler.ExecuteAsync(new AuthenticateUserCommand(req.Email, req.Password), ct);
        if (result is null)
        {
            loginThrottling.RecordFailure(req.Email);
            await Send.UnauthorizedAsync(ct);
            return;
        }

        loginThrottling.RecordSuccess(req.Email);
        await Send.OkAsync(new LoginResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.ExpiresAtUtc,
            RefreshToken = result.RefreshToken,
            RefreshTokenExpiresAtUtc = result.RefreshTokenExpiresAtUtc,
            User = result.User
        }, cancellation: ct);
    }

    private void SetRetryAfterHeader(LoginThrottleDecision decision)
    {
        if (decision.RetryAfter is null)
        {
            return;
        }

        var seconds = Math.Max(1, (int)Math.Ceiling(decision.RetryAfter.Value.TotalSeconds));
        HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
    }
}
