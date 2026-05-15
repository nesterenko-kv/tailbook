namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);