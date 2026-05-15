using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Staff.Api.Admin.AddTimeBlock;

public sealed class AddTimeBlockRequestValidator : Validator<AddTimeBlockRequest>
{
    public AddTimeBlockRequestValidator()
    {
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}