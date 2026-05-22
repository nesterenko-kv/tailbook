namespace Tailbook.Modules.VisitOperations.Application.VisitOperations.Models;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);