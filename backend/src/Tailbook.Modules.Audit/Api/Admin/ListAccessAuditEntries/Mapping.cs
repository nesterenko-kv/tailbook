using Tailbook.Modules.Audit.Application.AccessAuditEntries.Models;
using Tailbook.Modules.Audit.Application.Common.Pagination;

namespace Tailbook.Modules.Audit.Api.Admin.ListAccessAuditEntries;

internal static class Mapping
{
    public static ListAccessAuditEntriesResponse ToResponse(this PagedResult<AccessAuditEntryReadModel> result)
    {
        return new ListAccessAuditEntriesResponse
        {
            Items = result.Items.Select(x => new AccessAuditItemResponse
            {
                Id = x.Id,
                ActorUserId = x.ActorUserId,
                ResourceType = x.ResourceType,
                ResourceId = x.ResourceId,
                ActionCode = x.ActionCode,
                HappenedAtUtc = x.HappenedAtUtc
            }).ToArray(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}
