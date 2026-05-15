using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicPetPayloadValidator : AbstractValidator<PublicPetPayload>
{
    public PublicPetPayloadValidator()
    {
        RuleFor(x => x).Must(x => x.PetId.HasValue || (x.AnimalTypeId.HasValue && x.BreedId.HasValue))
            .WithMessage("Provide a saved petId or choose both animal type and breed.");
        RuleFor(x => x.PetName).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}