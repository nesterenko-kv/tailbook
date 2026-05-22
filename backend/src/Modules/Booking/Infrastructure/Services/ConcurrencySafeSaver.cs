using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Booking.Infrastructure.Services;

public static class ConcurrencySafeSaver
{
    public static async Task<ErrorOr<Success>> SaveAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return Error.Conflict("Booking.ConcurrencyConflict", "The resource was modified by another request. Reload and retry.");
        }
    }
}
