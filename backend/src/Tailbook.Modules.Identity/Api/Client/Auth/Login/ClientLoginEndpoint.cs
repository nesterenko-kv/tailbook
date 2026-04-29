using System.Globalization;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Client.Auth.Login;

public sealed class ClientLoginEndpoint(ClientPortalIdentityQueries identityQueries, LoginThrottlingService loginThrottling) : Endpoint<ClientLoginRequest, ClientLoginResponse>
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

        var result = await identityQueries.AuthenticateClientAsync(req.Email, req.Password, ct);
        if (result is null)
        {
            loginThrottling.RecordFailure(req.Email);
            await Send.UnauthorizedAsync(ct);
            return;
        }

        loginThrottling.RecordSuccess(req.Email);
        await Send.OkAsync(new ClientLoginResponse
        {
            AccessToken = result.AccessToken,
            ExpiresAtUtc = result.ExpiresAtUtc,
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

public sealed class ClientLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class ClientLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public AuthenticatedUserView User { get; set; } = default!;
}

public sealed class ClientLoginRequestValidator : Validator<ClientLoginRequest>
{
    public ClientLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}
