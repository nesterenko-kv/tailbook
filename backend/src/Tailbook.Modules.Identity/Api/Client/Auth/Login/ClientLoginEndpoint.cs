using System.Globalization;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Api.Auth.BrowserSessions;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Client.Auth.Login;

public sealed class ClientLoginEndpoint(
    IAuthenticateUserService authenticateUserHandler,
    ILoginThrottlingService loginThrottling,
    BrowserSessionService browserSessionService) : Endpoint<ClientLoginRequest, ClientLoginResponse>
{
    public override void Configure()
    {
        Post("/api/client/auth/login");
        AllowAnonymous();
        Description(x => x.WithTags("Client Portal Identity"));
    }

    public override async Task HandleAsync(ClientLoginRequest req, CancellationToken ct)
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

        var result = await authenticateUserHandler.AuthenticateAsync(
            req.Email,
            req.Password,
            requireClientPortalAccess: true,
            enforceMfa: false,
            requestIpAddress: null,
            userAgent: null,
            deviceTrustToken: null,
            cancellationToken: ct);
        if (result.IsError)
        {
            loginThrottling.RecordFailure(req.Email);
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        if (result.Value is not AuthenticationSucceededResult { Login: var login })
        {
            throw new InvalidOperationException("Client portal login does not support MFA challenges.");
        }

        var browserSession = browserSessionService.ApplyClientSession(HttpContext, login);
        if (browserSession.IsError)
        {
            await Send.ResultAsync(browserSession.Errors.ToHttpResult());
            return;
        }

        loginThrottling.RecordSuccess(req.Email);
        await Send.OkAsync(new ClientLoginResponse
        {
            AccessToken = login.AccessToken,
            ExpiresAt = login.ExpiresAt,
            RefreshToken = browserSession.Value.IncludeRefreshTokenInResponse ? login.RefreshToken : null,
            RefreshTokenExpiresAt = login.RefreshTokenExpiresAt,
            User = login.User
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

public sealed class ClientLoginRequestValidator : Validator<ClientLoginRequest>
{
    public ClientLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}
