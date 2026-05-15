using FastEndpoints;
using FluentValidation;

namespace Tailbook.Modules.Identity.Api.Admin.AssignRoles;

public sealed class AssignRolesRequestValidator : Validator<AssignRolesRequest>
{
    public AssignRolesRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
