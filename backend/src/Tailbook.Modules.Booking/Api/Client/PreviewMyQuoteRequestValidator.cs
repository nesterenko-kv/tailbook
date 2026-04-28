using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class PreviewMyQuoteRequestValidator : Validator<PreviewMyQuoteRequest>
{
    public PreviewMyQuoteRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new PreviewMyQuoteItemRequestValidator());
    }
}