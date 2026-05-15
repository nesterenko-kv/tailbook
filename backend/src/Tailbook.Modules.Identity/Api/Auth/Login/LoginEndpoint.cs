using System.Globalization;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Api.Auth.BrowserSessions;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Auth.Login;

public sealed class LoginEndpoint(
    ILoginThrottlingService loginThrottling,
    IAuthenticateUserService authenticateUserHandler,
    BrowserSessionService browserSessionService) : Endpoint<LoginRequest, LoginResponse>
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

        var deviceTrustToken = HttpContext.Request.Headers["X-Tailbook-Device-Trust"].FirstOrDefault();

        var result = await authenticateUserHandler.AuthenticateAsync(
            req.Email,
            req.Password,
            requireClientPortalAccess: false,
            enforceMfa: true,
            requestIpAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(),
            deviceTrustToken: deviceTrustToken,
            cancellationToken: ct);
        if (result.IsError)
        {
            loginThrottling.RecordFailure(req.Email);
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        loginThrottling.RecordSuccess(req.Email);
        switch (result.Value)
        {
            case AuthenticationSucceededResult success:
                var session = browserSessionService.ApplyIdentitySession(HttpContext, success.Login);
                if (session.IsError)
                {
                    await Send.ResultAsync(session.Errors.ToHttpResult());
                    return;
                }

                await Send.OkAsync(
                    LoginResponseMapper.FromLoginResult(success.Login, session.Value.IncludeRefreshTokenInResponse),
                    cancellation: ct);
                return;
            case AuthenticationMfaRequiredResult challenge:
                await Send.OkAsync(LoginResponseMapper.FromMfaChallenge(challenge), cancellation: ct);
                return;
            default:
                throw new InvalidOperationException("Unsupported authentication result.");
        }
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
