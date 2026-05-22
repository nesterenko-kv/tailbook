namespace Tailbook.Modules.Identity.Api.Admin.CreateUser;

public sealed class CreateUserResponse
{
    public Guid Id { get; set; }
    public string SubjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}
