using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Identity.Api.Admin.ListUsers;

public sealed class ListUsersEndpoint(IdentityQueries identityQueries)
    : Endpoint<ListUsersRequest, ListUsersResponse>
{
    public override void Configure()
    {
        Get("/api/admin/iam/users");
        Description(x => x.WithTags("Admin IAM"));
        PermissionsAll("iam.users.read");
    }

    public override async Task HandleAsync(ListUsersRequest req, CancellationToken ct)
    {
        var result = await identityQueries.ListUsersAsync(req.Page, req.PageSize, ct);

        await Send.OkAsync(new ListUsersResponse
        {
            Items = result.Items.Select(x => new UserItemResponse
            {
                Id = x.Id,
                SubjectId = x.SubjectId,
                Email = x.Email,
                DisplayName = x.DisplayName,
                Status = x.Status,
                Roles = x.Roles,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            }).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        }, cancellation: ct);
    }
}
