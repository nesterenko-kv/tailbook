using FastEndpoints;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicBookableOffersRequestValidator : Validator<PublicBookableOffersRequest>
{
    public PublicBookableOffersRequestValidator()
    {
        RuleFor(x => x.Pet).SetValidator(new PublicPetPayloadValidator());
    }
}