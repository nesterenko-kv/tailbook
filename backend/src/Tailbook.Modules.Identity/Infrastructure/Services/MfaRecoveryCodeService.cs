using System.Text.Json;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Infrastructure.Options;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class MfaRecoveryCodeService(
    AppDbContext dbContext,
    PasswordHasher passwordHasher,
    IAuditTrailService auditTrailService,
    IOptions<MfaRecoveryCodeOptions> optionsAccessor,
    TimeProvider timeProvider) : IMfaRecoveryCodeService
{
    private const string ModuleCode = "identity";
    private const string RecoveryCodeBatchEntityType = "iam_mfa_recovery_code_batch";
    private const string MfaRecoveryEntityType = "iam_mfa_recovery";
    public async Task<ErrorOr<MfaRecoveryCodeGenerationResult>> GenerateRecoveryCodesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<IdentityUser>()
            .SingleOrDefaultAsync(x => x.Id == userId && x.Status == UserStatusCodes.Active, cancellationToken);
        if (user is null)
        {
            return IdentityErrors.UserNotFound();
        }

        var hasEnabledMfaFactor = await dbContext.Set<IdentityMfaFactor>()
            .AnyAsync(x => x.UserId == userId && x.Status == MfaFactorStatusCodes.Enabled, cancellationToken);
        if (!hasEnabledMfaFactor)
        {
            return IdentityErrors.MfaFactorNotFound();
        }

        var options = optionsAccessor.Value;
        var utcNow = timeProvider.GetUtcNow();
        var batchId = Guid.NewGuid();
        var existingActiveCodes = await dbContext.Set<IdentityMfaRecoveryCode>()
            .Where(x => x.UserId == userId && x.ConsumedAt == null && x.InvalidatedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var existingCode in existingActiveCodes)
        {
            existingCode.InvalidatedAt = utcNow;
        }

        var rawCodes = GenerateUniqueCodes(options.CodeCount, options.CodeLength);
        foreach (var rawCode in rawCodes)
        {
            var normalizedCode = MfaRecoveryCodeHelpers.NormalizeRecoveryCode(rawCode);
            dbContext.Set<IdentityMfaRecoveryCode>().Add(new IdentityMfaRecoveryCode
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BatchId = batchId,
                CodeHash = passwordHasher.Hash(normalizedCode),
                CodeSuffix = MfaRecoveryCodeHelpers.GetCodeSuffix(normalizedCode),
                CreatedAt = utcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordGenerationAuditAsync(
            userId,
            batchId,
            existingActiveCodes.Count,
            rawCodes.Count,
            utcNow,
            cancellationToken);

        return new MfaRecoveryCodeGenerationResult(userId, batchId, rawCodes, rawCodes.Count, utcNow);
    }

    public async Task<MfaRecoveryCodeStatus> GetRecoveryCodeStatusAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var codes = await dbContext.Set<IdentityMfaRecoveryCode>()
            .Where(x => x.UserId == userId)
            .ToListAsync(cancellationToken);
        var activeCodeCount = codes.Count(x => x.ConsumedAt == null && x.InvalidatedAt == null);
        var lastGeneratedAt = codes.Count == 0 ? null : (DateTimeOffset?)codes.Max(x => x.CreatedAt);
        return new MfaRecoveryCodeStatus(userId, activeCodeCount, lastGeneratedAt);
    }

    public async Task<ErrorOr<MfaRecoveryCodeConsumptionResult>> ConsumeRecoveryCodeAsync(
        Guid userId,
        string code,
        Guid? challengeId,
        CancellationToken cancellationToken)
    {
        var normalizedCode = MfaRecoveryCodeHelpers.NormalizeRecoveryCode(code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return IdentityErrors.InvalidMfaRecoveryCode();
        }

        var activeCodes = await dbContext.Set<IdentityMfaRecoveryCode>()
            .Where(x => x.UserId == userId && x.ConsumedAt == null && x.InvalidatedAt == null)
            .ToListAsync(cancellationToken);
        var matchedCode = activeCodes.FirstOrDefault(x => passwordHasher.Verify(normalizedCode, x.CodeHash));
        if (matchedCode is null)
        {
            return IdentityErrors.InvalidMfaRecoveryCode();
        }

        var utcNow = timeProvider.GetUtcNow();
        matchedCode.ConsumedAt = utcNow;
        matchedCode.ConsumedChallengeId = challengeId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MfaRecoveryCodeConsumptionResult(matchedCode.Id, userId, challengeId, utcNow);
    }

    public async Task<ErrorOr<MfaRecoveryResetResult>> ResetMfaRecoveryAsync(
        Guid userId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (userId == actorUserId)
        {
            return IdentityErrors.MfaRecoverySelfResetNotAllowed();
        }

        var userExists = await dbContext.Set<IdentityUser>()
            .AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            return IdentityErrors.UserNotFound();
        }

        var utcNow = timeProvider.GetUtcNow();
        var enabledFactors = await dbContext.Set<IdentityMfaFactor>()
            .Where(x => x.UserId == userId && x.Status == MfaFactorStatusCodes.Enabled)
            .ToListAsync(cancellationToken);
        var activeRecoveryCodes = await dbContext.Set<IdentityMfaRecoveryCode>()
            .Where(x => x.UserId == userId && x.ConsumedAt == null && x.InvalidatedAt == null)
            .ToListAsync(cancellationToken);
        var outstandingChallenges = await dbContext.Set<IdentityMfaChallenge>()
            .Where(x => x.UserId == userId && x.ConsumedAt == null && x.InvalidatedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var factor in enabledFactors)
        {
            factor.Status = MfaFactorStatusCodes.Disabled;
            factor.DisabledAt = utcNow;
        }

        foreach (var recoveryCode in activeRecoveryCodes)
        {
            recoveryCode.InvalidatedAt = utcNow;
        }

        foreach (var challenge in outstandingChallenges)
        {
            challenge.Invalidate(utcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecordResetAuditAsync(
            userId,
            actorUserId,
            enabledFactors.Count,
            activeRecoveryCodes.Count,
            outstandingChallenges.Count,
            utcNow,
            cancellationToken);

        return new MfaRecoveryResetResult(
            userId,
            enabledFactors.Count,
            activeRecoveryCodes.Count,
            outstandingChallenges.Count,
            utcNow);
    }

    private static IReadOnlyCollection<string> GenerateUniqueCodes(int count, int length)
    {
        var codes = new List<string>(count);
        var normalizedCodes = new HashSet<string>(StringComparer.Ordinal);
        while (codes.Count < count)
        {
            var normalizedCode = MfaRecoveryCodeHelpers.GenerateNormalizedCode(length);
            if (!normalizedCodes.Add(normalizedCode))
            {
                continue;
            }

            codes.Add(MfaRecoveryCodeHelpers.FormatRecoveryCode(normalizedCode));
        }

        return codes;
    }

    private ValueTask RecordGenerationAuditAsync(
        Guid userId,
        Guid batchId,
        int invalidatedCodeCount,
        int generatedCodeCount,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var actionCode = invalidatedCodeCount > 0
            ? "MFA_RECOVERY_CODES_REGENERATED"
            : "MFA_RECOVERY_CODES_GENERATED";

        return auditTrailService.RecordAsync(
            ModuleCode,
            RecoveryCodeBatchEntityType,
            batchId.ToString("D"),
            actionCode,
            userId,
            null,
            JsonSerializer.Serialize(new
            {
                userId,
                batchId,
                generatedCodeCount,
                invalidatedCodeCount,
                createdAt
            }),
            cancellationToken);
    }

    private ValueTask RecordResetAuditAsync(
        Guid userId,
        Guid actorUserId,
        int disabledFactorCount,
        int invalidatedRecoveryCodeCount,
        int invalidatedChallengeCount,
        DateTimeOffset resetAt,
        CancellationToken cancellationToken)
    {
        return auditTrailService.RecordAsync(
            ModuleCode,
            MfaRecoveryEntityType,
            userId.ToString("D"),
            "MFA_RECOVERY_RESET",
            actorUserId,
            JsonSerializer.Serialize(new
            {
                userId,
                enabledFactorCount = disabledFactorCount,
                activeRecoveryCodeCount = invalidatedRecoveryCodeCount,
                outstandingChallengeCount = invalidatedChallengeCount
            }),
            JsonSerializer.Serialize(new
            {
                userId,
                disabledFactorCount,
                invalidatedRecoveryCodeCount,
                invalidatedChallengeCount,
                resetAt
            }),
            cancellationToken);
    }
}
