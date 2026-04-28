using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Identity.Domain;

namespace Tailbook.Modules.Identity.Application;

public sealed class IdentityReferenceServices(AppDbContext dbContext)
    : IUserReferenceValidationService,
      IClientPortalActorService
{
    public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<IdentityUser>().AnyAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task<ClientPortalActor?> GetActorAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<IdentityUser>().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null or {ClientId: null} or {ContactPersonId: null})
        {
            return null;
        }

        return new ClientPortalActor(user.Id, user.ClientId.Value, user.ContactPersonId.Value, user.Email, user.DisplayName);
    }
}
