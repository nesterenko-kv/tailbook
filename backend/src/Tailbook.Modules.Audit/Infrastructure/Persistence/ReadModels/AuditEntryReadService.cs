using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Audit.Application.AuditEntries.Models;
using Tailbook.Modules.Audit.Application.AuditEntries.Queries;
using Tailbook.Modules.Audit.Application.Common.Pagination;

namespace Tailbook.Modules.Audit.Infrastructure.Persistence.ReadModels;

public sealed class AuditEntryReadService(AppDbContext dbContext) : IAuditEntryReadService
{
    public async Task<PagedResult<AuditEntryReadModel>> ListAsync(ListAuditEntriesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize switch { <= 0 => 20, > 100 => 100, _ => request.PageSize };

        var query = dbContext.Set<AuditEntry>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.ModuleCode)) query = query.Where(x => x.ModuleCode == request.ModuleCode!.Trim());
        if (!string.IsNullOrWhiteSpace(request.EntityType)) query = query.Where(x => x.EntityType == request.EntityType!.Trim());
        if (!string.IsNullOrWhiteSpace(request.EntityId)) query = query.Where(x => x.EntityId == request.EntityId!.Trim());

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.HappenedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditEntryReadModel(
                x.Id,
                x.ModuleCode,
                x.EntityType,
                x.EntityId,
                x.ActionCode,
                x.ActorUserId,
                x.HappenedAtUtc,
                x.BeforeJson,
                x.AfterJson))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntryReadModel>(items, page, pageSize, totalCount);
    }
}
