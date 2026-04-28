using FastEndpoints;

namespace Tailbook.Modules.Identity.Application;

public sealed record RegisterClientPortalUserCommand(string DisplayName, string FirstName, string? LastName, string Email, string Password, string? Phone, string? Instagram) : ICommand;
