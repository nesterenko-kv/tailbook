using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Catalog.Api.Admin.AddOfferVersionComponent;

public sealed class AddOfferVersionComponentRequestValidator : Validator<AddOfferVersionComponentRequest>
{
    public AddOfferVersionComponentRequestValidator()
    {
        RuleFor(x => x.VersionId).NotEmpty();
        RuleFor(x => x.ProcedureId).NotEmpty();
        RuleFor(x => x.ComponentRole).NotEmpty().MaximumLength(32);
        RuleFor(x => x.SequenceNo).GreaterThan(0);
    }
}