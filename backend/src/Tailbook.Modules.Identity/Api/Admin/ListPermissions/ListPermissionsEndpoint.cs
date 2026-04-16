using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Admin.ListPermissions;

public sealed class ListPermissionsEndpoint(ICurrentUser currentUser, IIdentityAccessPolicy accessPolicy, IdentityQueries identityQueries)
    : EndpointWithoutRequest<IReadOnlyCollection<PermissionItemResponse>>
{
    public override void Configure()
    {
        Get("/api/admin/iam/permissions");
        Description(x => x.WithTags("Admin IAM"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await HttpContext.Response.CompleteAsync();
            return;
        }

        if (!accessPolicy.CanReadRoles(currentUser))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            await HttpContext.Response.CompleteAsync();
            return;
        }

        var items = await identityQueries.ListPermissionsAsync(ct);
        await Send.OkAsync(items.Select(x => new PermissionItemResponse
        {
            Id = x.Id,
            Code = x.Code,
            DisplayName = x.DisplayName
        }).ToArray(), cancellation: ct);
    }
}

public sealed class PermissionItemResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
