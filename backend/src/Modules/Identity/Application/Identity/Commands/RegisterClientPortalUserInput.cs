namespace Tailbook.Modules.Identity.Application.Identity.Commands;

public sealed record RegisterClientPortalUserInput(string DisplayName, string FirstName, string? LastName, string Email, string Password, string? Phone, string? Instagram);
