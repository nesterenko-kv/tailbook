using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Catalog.Api.Admin.CreateProcedure;

public sealed class CreateProcedureRequestValidator : Validator<CreateProcedureRequest>
{
    public CreateProcedureRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}