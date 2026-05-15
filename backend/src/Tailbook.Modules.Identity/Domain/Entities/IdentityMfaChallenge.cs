namespace Tailbook.Modules.Identity.Domain.Entities;

public sealed class IdentityMfaChallenge
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FactorId { get; set; }
    public string FactorType { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }
    public int FailedAttemptCount { get; set; }
    public DateTimeOffset? LastFailedAt { get; set; }
    public string? RequestIpAddress { get; set; }
    public string? UserAgent { get; set; }
}
