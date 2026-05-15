using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.VisitOperations.Api.Admin.ApplyVisitAdjustment;

public sealed class ApplyVisitAdjustmentRequestValidator : Validator<ApplyVisitAdjustmentRequest>
{
    public ApplyVisitAdjustmentRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.Sign).Must(x => x is -1 or 1);
        RuleFor(x => x.Amount).GreaterThan(0m);
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}