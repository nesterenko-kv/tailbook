namespace Tailbook.Modules.Identity.Api.Admin.CreateUser;

public sealed class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public IReadOnlyCollection<string> RoleCodes { get; set; } = [];
}
