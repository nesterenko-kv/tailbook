using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Identity.Domain.Events;

namespace Tailbook.Modules.Identity.Domain.Entities;

public sealed class IdentityPasswordResetToken : AggregateRoot
{
    private IdentityPasswordResetToken()
    {
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public static IdentityPasswordResetToken Create(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string email,
        string displayName,
        string protectedResetLink)
    {
        var utcCreatedAt = createdAt.ToUniversalTime();
        var entity = new IdentityPasswordResetToken
        {
            Id = id,
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt.ToUniversalTime(),
            CreatedAt = utcCreatedAt
        };

        entity.RaiseDomainEvent(new PasswordResetRequestedDomainEvent(
            Guid.NewGuid(),
            utcCreatedAt,
            email,
            displayName,
            protectedResetLink,
            entity.ExpiresAt));

        return entity;
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        UsedAt = usedAt.ToUniversalTime();
    }
}
