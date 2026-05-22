namespace Tailbook.Modules.Identity.Api.Client.Auth.Register;

public sealed class ClientRegisterRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Instagram { get; set; }
}
