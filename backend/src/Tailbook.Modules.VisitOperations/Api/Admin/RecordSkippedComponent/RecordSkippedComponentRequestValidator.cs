using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.VisitOperations.Api.Admin.RecordSkippedComponent;

public sealed class RecordSkippedComponentRequestValidator : Validator<RecordSkippedComponentRequest>
{
    public RecordSkippedComponentRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.VisitExecutionItemId).NotEmpty();
        RuleFor(x => x.OfferVersionComponentId).NotEmpty();
        RuleFor(x => x.OmissionReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}