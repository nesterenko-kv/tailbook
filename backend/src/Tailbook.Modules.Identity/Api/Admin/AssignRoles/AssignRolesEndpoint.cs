using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Admin.AssignRoles;

public sealed class AssignRolesEndpoint(IdentityQueries identityQueries)
    : Endpoint<AssignRolesRequest, AssignRolesResponse>
{
    public override void Configure()
    {
        Post("/api/admin/iam/users/{id:guid}/roles");
        Description(x => x.WithTags("Admin IAM"));
        PermissionsAll("iam.roles.assign");
    }

    public override async Task HandleAsync(AssignRolesRequest req, CancellationToken ct)
    {
        try
        {
            var user = await identityQueries.AssignRolesAsync(req.Id, req.RoleCodes, req.ActorUserId, ct);
            if (user is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.OkAsync(new AssignRolesResponse
            {
                Id = user.Id,
                Email = user.Email,
                Roles = user.Roles,
                Permissions = user.Permissions
            }, cancellation: ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }

}
