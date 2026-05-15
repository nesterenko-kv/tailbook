using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Staff.Api.Admin.CheckAvailability;

public sealed class CheckAvailabilityRequestValidator : Validator<CheckAvailabilityRequest>
{
    public CheckAvailabilityRequestValidator()
    {
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.ReservedMinutes).GreaterThan(0).LessThanOrEqualTo(1440);
        RuleFor(x => x.OfferIds).NotEmpty();
    }
}