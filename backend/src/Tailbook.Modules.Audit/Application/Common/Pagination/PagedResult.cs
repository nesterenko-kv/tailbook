namespace Tailbook.Modules.Audit.Application.Common.Pagination;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
