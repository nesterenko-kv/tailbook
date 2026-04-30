using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Audit.Application.AccessAuditEntries.Models;
using Tailbook.Modules.Audit.Application.AccessAuditEntries.Queries;
using Tailbook.Modules.Audit.Application.Common.Pagination;

namespace Tailbook.Modules.Audit.Infrastructure.Persistence.ReadModels;

public sealed class AccessAuditEntryReadService(AppDbContext dbContext) : IAccessAuditEntryQueries
{
    public async Task<PagedResult<AccessAuditEntryReadModel>> ListAsync(ListAccessAuditEntriesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            <= 0 => 20,
            > 100 => 100,
            _ => request.PageSize
        };

        var query = dbContext.Set<AccessAuditEntry>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ResourceType))
            query = query.Where(x => x.ResourceType == request.ResourceType);

        if (!string.IsNullOrWhiteSpace(request.ResourceId))
            query = query.Where(x => x.ResourceId == request.ResourceId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.HappenedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AccessAuditEntryReadModel(
                x.Id,
                x.ActorUserId,
                x.ResourceType,
                x.ResourceId,
                x.ActionCode,
                x.HappenedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<AccessAuditEntryReadModel>(items, page, pageSize, totalCount);
    }
}
