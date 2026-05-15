using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Pets.Api.Admin.ListPets;

public sealed class ListPetsRequestValidator : Validator<ListPetsRequest>
{
    public ListPetsRequestValidator()
    {
        RuleFor(x => x.Search).MaximumLength(128);
        RuleFor(x => x.AnimalTypeCode).MaximumLength(64);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}