using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Identity.Api.Admin.GetUserById;

public sealed class GetUserByIdEndpoint(
    IIdentityReadService identityReadService,
    IEntityScopeService entityScopeService) : Endpoint<GetUserByIdRequest, GetUserByIdResponse>
{
    public override void Configure()
    {
        Get("/api/admin/iam/users/{id:guid}");
        Description(x => x.WithTags("Admin IAM"));
        PermissionsAll("iam.users.read");
    }

    public override async Task HandleAsync(GetUserByIdRequest req, CancellationToken ct)
    {
        var userResult = await identityReadService.GetUserAsync(req.Id, ct);
        if (userResult.IsError)
        {
            await Send.ResultAsync(userResult.Errors.ToHttpResult());
            return;
        }

        var scopeResult = await entityScopeService.VerifyAccessAsync(EntityScopeResourceTypes.IamUser, req.Id.ToString("D"), req.ActorUserId, ct);
        if (scopeResult.IsError)
        {
            await Send.ResultAsync(scopeResult.Errors.ToHttpResult());
            return;
        }

        var user = userResult.Value;
        await Send.OkAsync(new GetUserByIdResponse
        {
            Id = user.Id,
            SubjectId = user.SubjectId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Status = user.Status,
            Roles = user.Roles,
            Permissions = user.Permissions,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        }, cancellation: ct);
    }

}
