using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Catalog.Api.Admin.CreatePriceRuleSet;

public sealed class CreatePriceRuleSetRequestValidator : Validator<CreatePriceRuleSetRequest>
{
    public CreatePriceRuleSetRequestValidator()
    {
        RuleFor(x => x).Must(x => x.ValidTo is null || x.ValidFrom is null || x.ValidTo >= x.ValidFrom)
            .WithMessage("ValidTo must be greater than or equal to ValidFrom.");
    }
}