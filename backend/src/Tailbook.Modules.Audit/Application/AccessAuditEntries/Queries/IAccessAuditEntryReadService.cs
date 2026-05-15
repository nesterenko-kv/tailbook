using Tailbook.Modules.Audit.Application.AccessAuditEntries.Models;
using Tailbook.Modules.Audit.Application.Common.Pagination;

namespace Tailbook.Modules.Audit.Application.AccessAuditEntries.Queries;

public interface IAccessAuditEntryReadService
{
    Task<PagedResult<AccessAuditEntryReadModel>> ListAsync(ListAccessAuditEntriesQuery query, CancellationToken cancellationToken);
}
