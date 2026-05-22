using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Api.Auth.BrowserSessions;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Identity.Api.Auth.Login;

namespace Tailbook.Modules.Identity.Api.Auth.Mfa;

public sealed class VerifyMfaChallengeEndpoint(
    IMfaChallengeService mfaChallengeService,
    IIdentitySessionService identitySessionService,
    BrowserSessionService browserSessionService,
    IDeviceTrustService deviceTrustService) : Endpoint<VerifyMfaChallengeRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/identity/auth/mfa/verify");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(VerifyMfaChallengeRequest req, CancellationToken ct)
    {
        var verification = await mfaChallengeService.VerifyEmailOtpChallengeAsync(req.ChallengeId, req.Code, ct);
        if (verification.IsError)
        {
            await Send.ResultAsync(verification.Errors.ToHttpResult());
            return;
        }

        var userId = verification.Value.UserId;
        var surface = HttpContext.Request.Headers["X-Tailbook-Session-Surface"].FirstOrDefault() ?? BrowserSessionSurfaces.Admin;

        if (req.TrustDevice)
        {
            await deviceTrustService.IssueTrustTokenAsync(userId, surface, HttpContext.Request.Headers.UserAgent.ToString(), ct);
        }

        var session = await identitySessionService.CreateSessionAsync(userId, requireClientPortalAccess: false, ct);
        if (session.IsError)
        {
            await Send.ResultAsync(session.Errors.ToHttpResult());
            return;
        }

        var browserSession = browserSessionService.ApplyIdentitySession(HttpContext, session.Value);
        if (browserSession.IsError)
        {
            await Send.ResultAsync(browserSession.Errors.ToHttpResult());
            return;
        }

        await Send.OkAsync(
            LoginResponseMapper.FromLoginResult(session.Value, browserSession.Value.IncludeRefreshTokenInResponse),
            cancellation: ct);
    }
}

public sealed class VerifyMfaChallengeRequestValidator : Validator<VerifyMfaChallengeRequest>
{
    public VerifyMfaChallengeRequestValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MinimumLength(6).MaximumLength(10).Matches("^[0-9]+$");
    }
}
