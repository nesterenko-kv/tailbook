namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);