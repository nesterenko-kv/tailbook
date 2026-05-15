using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.VisitOperations.Api.Admin.CloseVisit;

public sealed class CloseVisitRequestValidator : Validator<CloseVisitRequest>
{
    public CloseVisitRequestValidator()
    {
        RuleFor(x => x.VisitId).NotEmpty();
    }
}