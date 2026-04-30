using Tailbook.Modules.Audit.Application.AuditEntries.Models;
using Tailbook.Modules.Audit.Application.Common.Pagination;

namespace Tailbook.Modules.Audit.Application.AuditEntries.Queries;

public interface IAuditEntryQueries
{
    Task<PagedResult<AuditEntryReadModel>> ListAsync(ListAuditEntriesQuery query, CancellationToken cancellationToken);
}
