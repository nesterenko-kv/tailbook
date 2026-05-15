using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

public sealed class CreateGroomerRequestValidator : Validator<CreateGroomerRequest>
{
    public CreateGroomerRequestValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
    }
}