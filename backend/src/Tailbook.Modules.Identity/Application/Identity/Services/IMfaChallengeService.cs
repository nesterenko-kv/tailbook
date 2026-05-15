using ErrorOr;

namespace Tailbook.Modules.Identity.Application.Identity.Services;

public interface IMfaChallengeService
{
    Task<ErrorOr<MfaChallengeCreationResult>> CreateEmailOtpChallengeAsync(
        Guid userId,
        string? requestIpAddress,
        string? userAgent,
        CancellationToken cancellationToken);

    Task<ErrorOr<MfaChallengeVerificationResult>> VerifyEmailOtpChallengeAsync(
        Guid challengeId,
        string code,
        CancellationToken cancellationToken);

    Task<ErrorOr<MfaChallengeVerificationResult>> VerifyRecoveryCodeChallengeAsync(
        Guid challengeId,
        string recoveryCode,
        CancellationToken cancellationToken);
}
