using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Identity.Domain.Events;

namespace Tailbook.Modules.Identity.Domain.Entities;

public sealed class IdentityMfaChallenge : AggregateRoot
{
    private IdentityMfaChallenge()
    {
    }

    public Guid UserId { get; private set; }
    public Guid FactorId { get; private set; }
    public string FactorType { get; private set; } = string.Empty;
    public string CodeHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public DateTimeOffset? InvalidatedAt { get; private set; }
    public int FailedAttemptCount { get; private set; }
    public DateTimeOffset? LastFailedAt { get; private set; }
    public string? RequestIpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public static IdentityMfaChallenge CreateEmailOtp(
        Guid id,
        Guid userId,
        Guid factorId,
        string factorType,
        string codeHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? requestIpAddress,
        string? userAgent,
        string email,
        string displayName,
        string protectedCode)
    {
        var utcCreatedAt = createdAt.ToUniversalTime();
        var entity = new IdentityMfaChallenge
        {
            Id = id,
            UserId = userId,
            FactorId = factorId,
            FactorType = factorType,
            CodeHash = codeHash,
            ExpiresAt = expiresAt.ToUniversalTime(),
            CreatedAt = utcCreatedAt,
            RequestIpAddress = requestIpAddress,
            UserAgent = userAgent
        };

        entity.RaiseDomainEvent(new MfaEmailOtpChallengeCreatedDomainEvent(
            Guid.NewGuid(),
            utcCreatedAt,
            email,
            displayName,
            entity.Id,
            protectedCode,
            entity.ExpiresAt));

        return entity;
    }

    public void Invalidate(DateTimeOffset invalidatedAt)
    {
        InvalidatedAt = invalidatedAt.ToUniversalTime();
    }

    public void RecordFailedAttempt(DateTimeOffset failedAt)
    {
        FailedAttemptCount += 1;
        LastFailedAt = failedAt.ToUniversalTime();
    }

    public void MarkConsumed(DateTimeOffset consumedAt)
    {
        ConsumedAt = consumedAt.ToUniversalTime();
    }
}
