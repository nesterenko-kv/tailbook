namespace Tailbook.Modules.Identity.Api.Client.Me;

public sealed class ClientMeResponse
{
    public Guid UserId { get; set; }
    public Guid ClientId { get; set; }
    public Guid ContactPersonId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}
