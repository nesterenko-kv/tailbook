using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicPreviewQuoteRequestValidator : Validator<PublicPreviewQuoteRequest>
{
    public PublicPreviewQuoteRequestValidator()
    {
        RuleFor(x => x.Pet).SetValidator(new PublicPetPayloadValidator());
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.OfferId).NotEmpty();
            item.RuleFor(x => x.ItemType).MaximumLength(32);
            item.RuleFor(x => x.RequestedNotes).MaximumLength(1000);
        });
    }
}