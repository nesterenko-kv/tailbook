using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Identity.Infrastructure.Services;

public sealed class ScopeAuthorizationService(AppDbContext dbContext) : IScopeAuthorizationService
{
    public async Task<bool> HasGlobalScopeAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRoleAssignment>()
            .AnyAsync(x => x.UserId == userId && x.ScopeType == "Global" && x.ScopeId == null, cancellationToken);
    }

    public async Task<List<UserScope>> GetUserScopesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<UserRoleAssignment>()
            .Where(x => x.UserId == userId && x.ScopeType != "Global")
            .Select(x => new UserScope(x.ScopeType, x.ScopeId))
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
