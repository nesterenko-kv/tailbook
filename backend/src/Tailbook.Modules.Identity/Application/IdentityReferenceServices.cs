using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Domain;

namespace Tailbook.Modules.Identity.Application;

public sealed class IdentityReferenceServices(AppDbContext dbContext) : IUserReferenceValidationService
{
    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<IdentityUser>().AnyAsync(x => x.Id == userId, cancellationToken);
    }
}
