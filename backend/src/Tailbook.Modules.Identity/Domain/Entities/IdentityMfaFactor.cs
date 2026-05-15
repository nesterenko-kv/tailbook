namespace Tailbook.Modules.Identity.Domain.Entities;

public sealed class IdentityMfaFactor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FactorType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TargetEmail { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EnabledAt { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
}
