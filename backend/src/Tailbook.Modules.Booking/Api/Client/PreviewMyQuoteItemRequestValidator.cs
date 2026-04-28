using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class PreviewMyQuoteItemRequestValidator : AbstractValidator<PreviewMyQuoteItemRequest>
{
    public PreviewMyQuoteItemRequestValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.ItemType).MaximumLength(32);
    }
}