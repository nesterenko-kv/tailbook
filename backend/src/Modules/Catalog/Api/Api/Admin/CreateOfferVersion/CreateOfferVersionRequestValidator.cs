using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateOfferVersion;

public sealed class CreateOfferVersionRequestValidator : Validator<CreateOfferVersionRequest>
{
    public CreateOfferVersionRequestValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.PolicyText).MaximumLength(4000);
        RuleFor(x => x.ChangeNote).MaximumLength(1000);
    }
}