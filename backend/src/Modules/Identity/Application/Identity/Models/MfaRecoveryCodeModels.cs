namespace Tailbook.Modules.Identity.Application.Identity.Models;

public sealed record MfaRecoveryCodeGenerationResult(
    Guid UserId,
    Guid BatchId,
    IReadOnlyCollection<string> RecoveryCodes,
    int ActiveCodeCount,
    DateTimeOffset CreatedAt);

public sealed record MfaRecoveryCodeStatus(
    Guid UserId,
    int ActiveCodeCount,
    DateTimeOffset? LastGeneratedAt);

public sealed record MfaRecoveryCodeConsumptionResult(
    Guid RecoveryCodeId,
    Guid UserId,
    Guid? ChallengeId,
    DateTimeOffset ConsumedAt);

public sealed record MfaRecoveryResetResult(
    Guid UserId,
    int DisabledFactorCount,
    int InvalidatedRecoveryCodeCount,
    int InvalidatedChallengeCount,
    DateTimeOffset ResetAt);
