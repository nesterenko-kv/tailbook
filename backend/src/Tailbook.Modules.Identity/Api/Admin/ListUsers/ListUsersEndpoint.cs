using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Identity.Application;

namespace Tailbook.Modules.Identity.Api.Admin.ListUsers;

public sealed class ListUsersEndpoint(ICurrentUser currentUser, IIdentityAccessPolicy accessPolicy, IdentityQueries identityQueries)
    : Endpoint<ListUsersRequest, ListUsersResponse>
{
    public override void Configure()
    {
        Get("/api/admin/iam/users");
        Description(x => x.WithTags("Admin IAM"));
    }

    public override async Task HandleAsync(ListUsersRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanReadUsers(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

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
