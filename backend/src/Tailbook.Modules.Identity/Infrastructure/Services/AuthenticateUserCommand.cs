using FastEndpoints;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed record AuthenticateUserCommand(string Email, string Password) : ICommand<LoginResult?>;
