namespace Tailbook.Modules.Identity.Domain.Entities;

public sealed class IdentityDeviceTrust
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DeviceTokenHash { get; set; } = string.Empty;
    public string Surface { get; set; } = string.Empty;
    public string? Label { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}
