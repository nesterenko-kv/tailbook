using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Customer.Api.Admin.ListClients;

public sealed class ListClientsRequestValidator : Validator<ListClientsRequest>
{
    public ListClientsRequestValidator()
    {
        RuleFor(x => x.Search).MaximumLength(128);
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(100);
    }
}
