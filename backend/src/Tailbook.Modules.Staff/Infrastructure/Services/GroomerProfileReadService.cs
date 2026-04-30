using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Staff.Infrastructure.Services;

public sealed class GroomerProfileReadService(AppDbContext dbContext) : IGroomerProfileReadService
{
    public async Task<GroomerProfileReadModel?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Groomer>()
            .Where(x => x.UserId == userId)
            .Select(x => new GroomerProfileReadModel(x.Id, x.UserId, x.DisplayName, x.Active))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<GroomerProfileReadModel?> GetByGroomerIdAsync(Guid groomerId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Groomer>()
            .Where(x => x.Id == groomerId)
            .Select(x => new GroomerProfileReadModel(x.Id, x.UserId, x.DisplayName, x.Active))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<GroomerProfileReadModel>> ListActiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<Groomer>()
            .Where(x => x.Active)
            .OrderBy(x => x.DisplayName)
            .Select(x => new GroomerProfileReadModel(x.Id, x.UserId, x.DisplayName, x.Active))
            .ToArrayAsync(cancellationToken);
    }
}
