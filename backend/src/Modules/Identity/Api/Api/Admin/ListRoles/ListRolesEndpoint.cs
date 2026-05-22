using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Identity.Api.Admin.ListRoles;

public sealed class ListRolesEndpoint(IIdentityReadService identityReadService)
    : EndpointWithoutRequest<IReadOnlyCollection<RoleItemResponse>>
{
    public override void Configure()
    {
        Get("/api/admin/iam/roles");
        Description(x => x.WithTags("Admin IAM"));
        PermissionsAll("iam.roles.read");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await identityReadService.ListRolesAsync(ct);
        await Send.OkAsync(items.Select(x => new RoleItemResponse
        {
            Id = x.Id,
            Code = x.Code,
            DisplayName = x.DisplayName,
            PermissionCodes = x.PermissionCodes
        }).ToArray(), cancellation: ct);
    }
}
