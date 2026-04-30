using Tailbook.Modules.Audit.Application.AuditEntries.Models;
using Tailbook.Modules.Audit.Application.Common.Pagination;

namespace Tailbook.Modules.Audit.Api.Admin.ListAuditEntries;

internal static class Mapping
{
    public static ListAuditEntriesResponse ToResponse(this PagedResult<AuditEntryReadModel> result)
    {
        return new ListAuditEntriesResponse
        {
            Items = result.Items.Select(x => new AuditEntryItemResponse
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
            }).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
