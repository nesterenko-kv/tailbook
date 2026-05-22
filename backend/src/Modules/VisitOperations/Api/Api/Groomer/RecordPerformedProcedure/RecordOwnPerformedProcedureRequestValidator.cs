using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.VisitOperations.Api.Groomer.RecordPerformedProcedure;

public sealed class RecordOwnPerformedProcedureRequestValidator : Validator<RecordOwnPerformedProcedureRequest>
{
    public RecordOwnPerformedProcedureRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.VisitExecutionItemId).NotEmpty();
        RuleFor(x => x.ProcedureId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}