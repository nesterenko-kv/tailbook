using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Admin.CreateUser;

public sealed class CreateUserEndpoint(ICurrentUser currentUser, IIdentityQueries identityQueries)
    : Endpoint<CreateUserRequest, CreateUserResponse>
{
    public override void Configure()
    {
        Post("/api/admin/iam/users");
        Description(x => x.WithTags("Admin IAM"));
        PermissionsAll("iam.users.write");
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        if (req.RoleCodes.Count > 0 && !currentUser.HasPermission("iam.roles.assign"))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var result = await identityQueries.CreateUserAsync(req.Email, req.DisplayName, req.Password, req.RoleCodes, req.ActorUserId, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var user = result.Value;
        await Send.ResponseAsync(new CreateUserResponse
        {
            Id = user.Id,
            SubjectId = user.SubjectId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Status = user.Status,
            Roles = user.Roles,
            Permissions = user.Permissions
        }, StatusCodes.Status201Created, ct);
    }

}
