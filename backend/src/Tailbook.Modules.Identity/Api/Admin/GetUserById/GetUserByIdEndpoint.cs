using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Admin.GetUserById;

public sealed class GetUserByIdEndpoint(
    ICurrentUser currentUser,
    IIdentityAccessPolicy accessPolicy,
    IdentityQueries identityQueries,
    IAccessAuditService accessAuditService) : Endpoint<GetUserByIdRequest, GetUserByIdResponse>
{
    public override void Configure()
    {
        Get("/api/admin/iam/users/{id:guid}");
        Description(x => x.WithTags("Admin IAM"));
    }

    public override async Task HandleAsync(GetUserByIdRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await HttpContext.Response.CompleteAsync();
            return;
        }

        if (!accessPolicy.CanReadUsers(currentUser))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            await HttpContext.Response.CompleteAsync();
            return;
        }

        var user = await identityQueries.GetUserAsync(req.Id, ct);
        if (user is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await HttpContext.Response.CompleteAsync();
            return;
        }

        await accessAuditService.RecordAsync("iam_user", req.Id.ToString("D"), "READ_DETAIL", ParseActorId(currentUser), ct);

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

    private static Guid? ParseActorId(ICurrentUser currentUser)
    {
        return Guid.TryParse(currentUser.UserId, out var actorId) ? actorId : null;
    }
}
