using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Staff.Infrastructure.Services;

public sealed class GroomerProfileReadService(AppDbContext dbContext) : IGroomerProfileReadService
{
    public async Task<ErrorOr<GroomerProfileReadModel>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var groomer = await dbContext.Set<Groomer>()
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new GroomerProfileReadModel(x.Id, x.UserId, x.DisplayName, x.Active))
            .SingleOrDefaultAsync(cancellationToken);

        if (groomer is null || !groomer.Active)
        {
            return Error.Forbidden("Staff.GroomerProfileRequired", "Current user is not linked to an active groomer profile.");
        }

        return groomer;
    }

    public async Task<GroomerProfileReadModel?> GetByGroomerIdAsync(Guid groomerId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Groomer>()
            .AsNoTracking()
            .Where(x => x.Id == groomerId)
            .Select(x => new GroomerProfileReadModel(x.Id, x.UserId, x.DisplayName, x.Active))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<GroomerProfileReadModel>> ListActiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<Groomer>()
            .AsNoTracking()
            .Where(x => x.Active)
            .OrderBy(x => x.DisplayName)
            .Select(x => new GroomerProfileReadModel(x.Id, x.UserId, x.DisplayName, x.Active))
            .ToArrayAsync(cancellationToken);
    }
}
