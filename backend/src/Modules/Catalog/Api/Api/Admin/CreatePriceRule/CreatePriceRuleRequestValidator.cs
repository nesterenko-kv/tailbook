using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRule;

public sealed class CreatePriceRuleRequestValidator : Validator<CreatePriceRuleRequest>
{
    public CreatePriceRuleRequestValidator()
    {
        RuleFor(x => x.RuleSetId).NotEmpty();
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FixedAmount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(8);
    }
}