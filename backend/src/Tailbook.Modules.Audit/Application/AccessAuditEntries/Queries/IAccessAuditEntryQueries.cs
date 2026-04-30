using Tailbook.Modules.Audit.Application.AccessAuditEntries.Models;
using Tailbook.Modules.Audit.Application.Common.Pagination;

namespace Tailbook.Modules.Audit.Application.AccessAuditEntries.Queries;

public interface IAccessAuditEntryQueries
{
    Task<PagedResult<AccessAuditEntryReadModel>> ListAsync(ListAccessAuditEntriesQuery query, CancellationToken cancellationToken);
}
