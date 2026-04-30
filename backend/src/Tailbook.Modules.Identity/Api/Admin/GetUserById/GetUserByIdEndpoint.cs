using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Identity.Api.Admin.GetUserById;

public sealed class GetUserByIdEndpoint(
    IIdentityReadService identityReadService,
    IAccessAuditService accessAuditService) : Endpoint<GetUserByIdRequest, GetUserByIdResponse>
{
    public override void Configure()
    {
        Get("/api/admin/iam/users/{id:guid}");
        Description(x => x.WithTags("Admin IAM"));
        PermissionsAll("iam.users.read");
    }

    public override async Task HandleAsync(GetUserByIdRequest req, CancellationToken ct)
    {
        var user = await identityReadService.GetUserAsync(req.Id, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await accessAuditService.RecordAsync("iam_user", req.Id.ToString("D"), "READ_DETAIL", req.ActorUserId, ct);

        await Send.OkAsync(new GetUserByIdResponse
        {
            Id = user.Id,
            SubjectId = user.SubjectId,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Status = user.Status,
            Roles = user.Roles,
            Permissions = user.Permissions,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        }, cancellation: ct);
    }

}
