namespace Tailbook.Modules.Identity.Api.Me;

public sealed class GetCurrentUserResponse
{
    public string SubjectId { get; set; } = string.Empty;
    public Guid? ClientId { get; set; }
    public Guid? ContactPersonId { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}
