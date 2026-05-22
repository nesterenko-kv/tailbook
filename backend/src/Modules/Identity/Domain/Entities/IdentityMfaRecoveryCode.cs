namespace Tailbook.Modules.Identity.Domain.Entities;

public sealed class IdentityMfaRecoveryCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid BatchId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public string CodeSuffix { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public Guid? ConsumedChallengeId { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }
}
