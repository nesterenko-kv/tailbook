using FastEndpoints;

namespace Tailbook.Modules.Identity.Application.Identity.Commands;

public sealed record AuthenticateUserCommand(string Email, string Password) : ICommand<LoginResult?>;
