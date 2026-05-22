using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateDurationRule;

public sealed class CreateDurationRuleRequestValidator : Validator<CreateDurationRuleRequest>
{
    public CreateDurationRuleRequestValidator()
    {
        RuleFor(x => x.RuleSetId).NotEmpty();
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BaseMinutes).GreaterThan(0);
        RuleFor(x => x.BufferBeforeMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BufferAfterMinutes).GreaterThanOrEqualTo(0);
    }
}