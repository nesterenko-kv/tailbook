using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Audit.Api.Admin.ListAuditEntries;

public sealed class ListAuditEntriesEndpoint(AppDbContext dbContext)
    : Endpoint<ListAuditEntriesRequest, ListAuditEntriesResponse>
{
    public override void Configure()
    {
        Get("/api/admin/audit");
        Description(x => x.WithTags("Audit"));
        PermissionsAll("audit.trail.read");
    }

    public override async Task HandleAsync(ListAuditEntriesRequest req, CancellationToken ct)
    {
        var page = req.Page <= 0 ? 1 : req.Page;
        var pageSize = req.PageSize switch { <= 0 => 20, > 100 => 100, _ => req.PageSize };

        var query = dbContext.Set<AuditEntry>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(req.ModuleCode)) query = query.Where(x => x.ModuleCode == req.ModuleCode!.Trim());
        if (!string.IsNullOrWhiteSpace(req.EntityType)) query = query.Where(x => x.EntityType == req.EntityType!.Trim());
        if (!string.IsNullOrWhiteSpace(req.EntityId)) query = query.Where(x => x.EntityId == req.EntityId!.Trim());

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.HappenedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditEntryItemResponse
            {
                Id = x.Id,
                ModuleCode = x.ModuleCode,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                ActionCode = x.ActionCode,
                ActorUserId = x.ActorUserId,
                HappenedAtUtc = x.HappenedAtUtc,
                BeforeJson = x.BeforeJson,
                AfterJson = x.AfterJson
            })
            .ToListAsync(ct);

        await Send.OkAsync(new ListAuditEntriesResponse { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount }, ct);
    }
}
