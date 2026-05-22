using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.VisitOperations.Api.Admin.RecordPerformedProcedure;

public sealed class RecordPerformedProcedureRequestValidator : Validator<RecordPerformedProcedureRequest>
{
    public RecordPerformedProcedureRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
        RuleFor(x => x.VisitExecutionItemId).NotEmpty();
        RuleFor(x => x.ProcedureId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}