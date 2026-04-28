using FastEndpoints;

namespace Tailbook.Modules.Identity.Application;

public sealed record AuthenticateUserCommand(string Email, string Password) : ICommand<LoginResult?>;
