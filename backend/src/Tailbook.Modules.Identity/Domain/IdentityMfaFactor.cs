namespace Tailbook.Modules.Identity.Domain;

public sealed class IdentityMfaFactor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FactorType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TargetEmail { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? EnabledAtUtc { get; set; }
    public DateTime? DisabledAtUtc { get; set; }
}
