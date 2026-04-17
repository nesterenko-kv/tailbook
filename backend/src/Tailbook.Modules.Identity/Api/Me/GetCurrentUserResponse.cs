namespace Tailbook.Modules.Identity.Api.Me;

public sealed class GetCurrentUserResponse
{
    public Guid? UserId { get; set; }
    public string SubjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid? ClientId { get; set; }
    public Guid? ContactPersonId { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}
