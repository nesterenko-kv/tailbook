using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Admin.CreateUser;

public sealed class CreateUserEndpoint(ICurrentUser currentUser, IIdentityAccessPolicy accessPolicy, IdentityQueries identityQueries)
    : Endpoint<CreateUserRequest, CreateUserResponse>
{
    public override void Configure()
    {
        Post("/api/admin/iam/users");
        Description(x => x.WithTags("Admin IAM"));
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        if (!accessPolicy.CanWriteUsers(currentUser) || (req.RoleCodes.Count > 0 && !accessPolicy.CanAssignRoles(currentUser)))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var user = await identityQueries.CreateUserAsync(req.Email, req.DisplayName, req.Password, req.RoleCodes, ParseActorId(currentUser), ct);
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
