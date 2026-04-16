using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Audit.Domain;

namespace Tailbook.Modules.Audit.Api.Admin.ListAccessAuditEntries;

public sealed class ListAccessAuditEntriesEndpoint(
    ICurrentUser currentUser,
    AppDbContext dbContext) : Endpoint<ListAccessAuditEntriesRequest, ListAccessAuditEntriesResponse>
{
    private const string AuditAccessReadPermission = "audit.access.read";

    public override void Configure()
    {
        Get("/api/admin/audit/access");
        Description(x => x.WithTags("Audit"));
    }

    public override async Task HandleAsync(ListAccessAuditEntriesRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await HttpContext.Response.CompleteAsync();
            return;
        }

        if (!currentUser.HasPermission(AuditAccessReadPermission))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            await HttpContext.Response.CompleteAsync();
            return;
        }

        var page = req.Page <= 0 ? 1 : req.Page;
        var pageSize = req.PageSize switch
        {
            <= 0 => 20,
            > 100 => 100,
            _ => req.PageSize
        };

        var query = dbContext.Set<AccessAuditEntry>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.ResourceType))
        {
            query = query.Where(x => x.ResourceType == req.ResourceType);
        }

        if (!string.IsNullOrWhiteSpace(req.ResourceId))
        {
            query = query.Where(x => x.ResourceId == req.ResourceId);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.HappenedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AccessAuditItemResponse
            {
                Id = x.Id,
                ActorUserId = x.ActorUserId,
                ResourceType = x.ResourceType,
                ResourceId = x.ResourceId,
                ActionCode = x.ActionCode,
                HappenedAtUtc = x.HappenedAtUtc
            })
            .ToListAsync(ct);

        await Send.OkAsync(new ListAccessAuditEntriesResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        }, cancellation: ct);
    }
}
