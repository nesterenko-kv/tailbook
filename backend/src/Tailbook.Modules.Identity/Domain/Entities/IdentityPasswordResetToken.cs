namespace Tailbook.Modules.Identity.Domain.Entities;

public sealed class IdentityPasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
