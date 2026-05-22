using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Pets.Api.Admin.RegisterPet;

public sealed class RegisterPetRequestValidator : Validator<RegisterPetRequest>
{
    public RegisterPetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.AnimalTypeCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.BreedId).NotEmpty();
        RuleFor(x => x.CoatTypeCode).MaximumLength(64);
        RuleFor(x => x.SizeCategoryCode).MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.WeightKg).GreaterThanOrEqualTo(0).When(x => x.WeightKg.HasValue);
    }
}