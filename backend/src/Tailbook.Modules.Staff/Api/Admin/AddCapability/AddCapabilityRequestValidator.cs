using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Staff.Api.Admin.AddCapability;

public sealed class AddCapabilityRequestValidator : Validator<AddCapabilityRequest>
{
    public AddCapabilityRequestValidator()
    {
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.CapabilityMode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.ReservedDurationModifierMinutes).InclusiveBetween(-240, 240);
    }
}