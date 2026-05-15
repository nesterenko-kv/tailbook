using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.Modules.Identity.Api.Auth.BrowserSessions;
using Tailbook.Modules.Identity.Application.Identity.Services;
using Tailbook.BuildingBlocks.Infrastructure.Http;
using Tailbook.Modules.Identity.Api.Auth.Login;

namespace Tailbook.Modules.Identity.Api.Auth.Mfa;

public sealed class VerifyMfaRecoveryCodeEndpoint(
    IMfaChallengeService mfaChallengeService,
    IIdentitySessionService identitySessionService,
    BrowserSessionService browserSessionService,
    IDeviceTrustService deviceTrustService) : Endpoint<VerifyMfaRecoveryCodeRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/identity/auth/mfa/recovery-code/verify");
        AllowAnonymous();
        Description(x => x.WithTags("Identity"));
    }

    public override async Task HandleAsync(VerifyMfaRecoveryCodeRequest req, CancellationToken ct)
    {
        var verification = await mfaChallengeService.VerifyRecoveryCodeChallengeAsync(req.ChallengeId, req.RecoveryCode, ct);
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

public sealed class VerifyMfaRecoveryCodeRequestValidator : Validator<VerifyMfaRecoveryCodeRequest>
{
    public VerifyMfaRecoveryCodeRequestValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.RecoveryCode).NotEmpty().MinimumLength(12).MaximumLength(64).Matches("^[A-Za-z0-9\\-\\s]+$");
    }
}
