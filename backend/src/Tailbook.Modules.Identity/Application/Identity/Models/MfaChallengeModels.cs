namespace Tailbook.Modules.Identity.Application.Identity.Models;

public sealed record MfaChallengeCreationResult(
    Guid ChallengeId,
    Guid UserId,
    Guid FactorId,
    string FactorType,
    string TargetEmail,
    string Code,
    DateTimeOffset ExpiresAt);

public sealed record MfaChallengeVerificationResult(
    Guid ChallengeId,
    Guid UserId,
    Guid FactorId,
    DateTimeOffset ConsumedAt);
