using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Admin.AssignRoles;

public sealed class AssignRolesEndpoint(ICurrentUser currentUser, IIdentityAccessPolicy accessPolicy, IdentityQueries identityQueries)
    : Endpoint<AssignRolesRequest, AssignRolesResponse>
{
    public override void Configure()
    {
        Post("/api/admin/iam/users/{id:guid}/roles");
        Description(x => x.WithTags("Admin IAM"));
    }

    public override async Task HandleAsync(AssignRolesRequest req, CancellationToken ct)
    {
        if (!accessPolicy.CanAssignRoles(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var user = await identityQueries.AssignRolesAsync(req.Id, req.RoleCodes, ParseActorId(currentUser), ct);
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

    private static Guid? ParseActorId(ICurrentUser currentUser)
    {
        return Guid.TryParse(currentUser.UserId, out var actorId) ? actorId : null;
    }
}
