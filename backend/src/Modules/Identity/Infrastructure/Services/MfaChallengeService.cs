using System.Security.Cryptography;
using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Abstractions.Security;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Infrastructure.Options;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class MfaChallengeService(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    ISensitivePayloadProtector sensitivePayloadProtector,
    IAuditTrailService auditTrailService,
    IOptions<MfaChallengeOptions> optionsAccessor,
    TimeProvider timeProvider) : IMfaChallengeService
{
    private const string ModuleCode = "identity";
    private const string MfaChallengeEntityType = "iam_mfa_challenge";
    private const string MfaRecoveryCodeEntityType = "iam_mfa_recovery_code";

    public async Task<ErrorOr<MfaChallengeCreationResult>> CreateEmailOtpChallengeAsync(
        Guid userId,
        string? requestIpAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.Id == userId && x.Status == UserStatusCodes.Active, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound();
        }

        var factor = await dbContext.Set<IdentityMfaFactor>()
            .Where(x => x.UserId == userId
                        && x.FactorType == MfaFactorTypes.EmailOtp
                        && x.Status == MfaFactorStatusCodes.Enabled)
            .OrderByDescending(x => x.EnabledAt ?? x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (factor is null)
        {
            return IdentityErrors.MfaFactorNotFound();
        }

        var options = optionsAccessor.Value;
        var utcNow = timeProvider.GetUtcNow();
        var activeChallenges = await dbContext.Set<IdentityMfaChallenge>()
            .Where(x => x.UserId == userId
                        && x.FactorId == factor.Id
                        && x.ConsumedAt == null
                        && x.InvalidatedAt == null
                        && x.ExpiresAt > utcNow)
            .ToListAsync(cancellationToken);

        foreach (var challenge in activeChallenges)
        {
            challenge.Invalidate(utcNow);
        }

        var code = GenerateNumericCode(options.CodeLength);
        var entity = IdentityMfaChallenge.CreateEmailOtp(
            Guid.NewGuid(),
            userId,
            factor.Id,
            MfaFactorTypes.EmailOtp,
            passwordHasher.Hash(code),
            utcNow.AddMinutes(options.ExpirationMinutes),
            utcNow,
            TrimToNull(requestIpAddress, 64),
            TrimToNull(userAgent, 512),
            user.Email,
            user.DisplayName,
            sensitivePayloadProtector.Protect(SensitivePayloadPurposes.MfaEmailOtpCode, code));

        dbContext.Set<IdentityMfaChallenge>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordChallengeAuditAsync(entity, "MFA_CHALLENGE_CREATED", null, cancellationToken);

        foreach (var invalidatedChallenge in activeChallenges)
        {
            await RecordChallengeAuditAsync(invalidatedChallenge, "MFA_CHALLENGE_INVALIDATED", "SupersededByNewChallenge", cancellationToken);
        }

        return new MfaChallengeCreationResult(
            entity.Id,
            user.Id,
            factor.Id,
            entity.FactorType,
            factor.TargetEmail,
            code,
            entity.ExpiresAt);
    }

    public async Task<ErrorOr<MfaChallengeVerificationResult>> VerifyEmailOtpChallengeAsync(
        Guid challengeId,
        string code,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return IdentityErrors.InvalidMfaChallengeCode();
        }

        var challenge = await dbContext.Set<IdentityMfaChallenge>()
            .SingleOrDefaultAsync(x => x.Id == challengeId, cancellationToken);
        if (challenge is null)
        {
            await RecordUnknownChallengeAuditAsync(challengeId, "MFA_CHALLENGE_VERIFY_REJECTED", "UnknownChallenge", cancellationToken);
            return IdentityErrors.InvalidMfaChallengeCode();
        }

        if (challenge.InvalidatedAt is not null || challenge.ConsumedAt is not null)
        {
            await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_VERIFY_REJECTED", challenge.InvalidatedAt is not null ? "InvalidatedChallenge" : "ConsumedChallenge", cancellationToken);
            return IdentityErrors.InvalidMfaChallengeCode();
        }

        var options = optionsAccessor.Value;
        if (challenge.FailedAttemptCount >= options.MaxFailedAttempts)
        {
            await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_ATTEMPTS_EXCEEDED", "AttemptLimitAlreadyReached", cancellationToken);
            return IdentityErrors.MfaChallengeAttemptsExceeded();
        }

        var utcNow = timeProvider.GetUtcNow();
        if (challenge.ExpiresAt <= utcNow)
        {
            await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_EXPIRED", null, cancellationToken);
            return IdentityErrors.ExpiredMfaChallenge();
        }

        if (!passwordHasher.Verify(code, challenge.CodeHash))
        {
            challenge.RecordFailedAttempt(utcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (challenge.FailedAttemptCount >= options.MaxFailedAttempts)
            {
                await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_ATTEMPTS_EXCEEDED", "AttemptLimitReached", cancellationToken);
                return IdentityErrors.MfaChallengeAttemptsExceeded();
            }

            await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_VERIFY_FAILED", null, cancellationToken);
            return IdentityErrors.InvalidMfaChallengeCode();
        }

        challenge.MarkConsumed(utcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_VERIFIED", null, cancellationToken);

        return new MfaChallengeVerificationResult(challenge.Id, challenge.UserId, challenge.FactorId, utcNow);
    }

    public async Task<ErrorOr<MfaChallengeVerificationResult>> VerifyRecoveryCodeChallengeAsync(
        Guid challengeId,
        string recoveryCode,
        CancellationToken cancellationToken)
    {
        var normalizedRecoveryCode = NormalizeRecoveryCode(recoveryCode);
        if (string.IsNullOrWhiteSpace(normalizedRecoveryCode))
        {
            return IdentityErrors.InvalidMfaRecoveryCode();
        }

        var challenge = await dbContext.Set<IdentityMfaChallenge>()
            .SingleOrDefaultAsync(x => x.Id == challengeId, cancellationToken);
        if (challenge is null)
        {
            await RecordUnknownChallengeAuditAsync(challengeId, "MFA_RECOVERY_CODE_VERIFY_REJECTED", "UnknownChallenge", cancellationToken);
            return IdentityErrors.InvalidMfaRecoveryCode();
        }

        if (challenge.InvalidatedAt is not null || challenge.ConsumedAt is not null)
        {
            await RecordChallengeAuditAsync(challenge, "MFA_RECOVERY_CODE_VERIFY_REJECTED", challenge.InvalidatedAt is not null ? "InvalidatedChallenge" : "ConsumedChallenge", cancellationToken);
            return IdentityErrors.InvalidMfaRecoveryCode();
        }

        var options = optionsAccessor.Value;
        if (challenge.FailedAttemptCount >= options.MaxFailedAttempts)
        {
            await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_ATTEMPTS_EXCEEDED", "RecoveryCodeAttemptLimitAlreadyReached", cancellationToken);
            return IdentityErrors.MfaChallengeAttemptsExceeded();
        }

        var utcNow = timeProvider.GetUtcNow();
        if (challenge.ExpiresAt <= utcNow)
        {
            await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_EXPIRED", "RecoveryCodeVerification", cancellationToken);
            return IdentityErrors.ExpiredMfaChallenge();
        }

        var activeRecoveryCodes = await dbContext.Set<IdentityMfaRecoveryCode>()
            .Where(x => x.UserId == challenge.UserId
                        && x.ConsumedAt == null
                        && x.InvalidatedAt == null)
            .ToListAsync(cancellationToken);
        var matchedRecoveryCode = activeRecoveryCodes
            .FirstOrDefault(x => passwordHasher.Verify(normalizedRecoveryCode, x.CodeHash));

        if (matchedRecoveryCode is null)
        {
            challenge.RecordFailedAttempt(utcNow);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (challenge.FailedAttemptCount >= options.MaxFailedAttempts)
            {
                await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_ATTEMPTS_EXCEEDED", "RecoveryCodeAttemptLimitReached", cancellationToken);
                return IdentityErrors.MfaChallengeAttemptsExceeded();
            }

            await RecordChallengeAuditAsync(challenge, "MFA_RECOVERY_CODE_VERIFY_FAILED", null, cancellationToken);
            return IdentityErrors.InvalidMfaRecoveryCode();
        }

        challenge.MarkConsumed(utcNow);
        matchedRecoveryCode.ConsumedAt = utcNow;
        matchedRecoveryCode.ConsumedChallengeId = challenge.Id;
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordChallengeAuditAsync(challenge, "MFA_CHALLENGE_VERIFIED_WITH_RECOVERY_CODE", null, cancellationToken);
        await RecordRecoveryCodeAuditAsync(matchedRecoveryCode, "MFA_RECOVERY_CODE_USED", null, cancellationToken);

        return new MfaChallengeVerificationResult(challenge.Id, challenge.UserId, challenge.FactorId, utcNow);
    }

    private static string GenerateNumericCode(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(chars);
    }

    private static string? TrimToNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string NormalizeRecoveryCode(string code)
    {
        return new string(code.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    private ValueTask RecordChallengeAuditAsync(
        IdentityMfaChallenge challenge,
        string actionCode,
        string? reason,
        CancellationToken cancellationToken)
    {
        return auditTrailService.RecordAsync(
            ModuleCode,
            MfaChallengeEntityType,
            challenge.Id.ToString("D"),
            actionCode,
            null,
            null,
            JsonSerializer.Serialize(new
            {
                challenge.UserId,
                challenge.FactorId,
                challenge.FactorType,
                challenge.ExpiresAt,
                challenge.ConsumedAt,
                challenge.InvalidatedAt,
                challenge.FailedAttemptCount,
                challenge.LastFailedAt,
                reason
            }),
            cancellationToken);
    }

    private ValueTask RecordUnknownChallengeAuditAsync(
        Guid challengeId,
        string actionCode,
        string reason,
        CancellationToken cancellationToken)
    {
        return auditTrailService.RecordAsync(
            ModuleCode,
            MfaChallengeEntityType,
            challengeId.ToString("D"),
            actionCode,
            null,
            null,
            JsonSerializer.Serialize(new { reason }),
            cancellationToken);
    }

    private ValueTask RecordRecoveryCodeAuditAsync(
        IdentityMfaRecoveryCode recoveryCode,
        string actionCode,
        string? reason,
        CancellationToken cancellationToken)
    {
        return auditTrailService.RecordAsync(
            ModuleCode,
            MfaRecoveryCodeEntityType,
            recoveryCode.Id.ToString("D"),
            actionCode,
            null,
            null,
            JsonSerializer.Serialize(new
            {
                recoveryCode.UserId,
                recoveryCode.BatchId,
                recoveryCode.CreatedAt,
                recoveryCode.ConsumedAt,
                recoveryCode.ConsumedChallengeId,
                recoveryCode.InvalidatedAt,
                reason
            }),
            cancellationToken);
    }
}
