using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CompleteVisit;

public sealed class CompleteVisitRequestValidator : Validator<CompleteVisitRequest>
{
    public CompleteVisitRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
    }
}