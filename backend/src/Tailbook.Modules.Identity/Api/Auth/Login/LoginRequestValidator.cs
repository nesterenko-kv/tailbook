using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Identity.Api.Auth.Login;

public sealed class LoginRequestValidator : Validator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
