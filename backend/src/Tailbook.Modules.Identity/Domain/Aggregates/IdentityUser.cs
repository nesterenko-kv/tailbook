namespace Tailbook.Modules.Identity.Domain.Aggregates;

public sealed class IdentityUser
{
    public Guid Id { get; set; }
    public string SubjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ClientId { get; set; }
    public Guid? ContactPersonId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
