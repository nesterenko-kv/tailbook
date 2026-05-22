using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class CreateCatalogDurationRuleSetCommandHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateCatalogDurationRuleSetCommand, DurationRuleSetView>
{
    public async Task<DurationRuleSetView> ExecuteAsync(CreateCatalogDurationRuleSetCommand command, CancellationToken cancellationToken)
    {
        var nextVersionNo = (await dbContext.Set<DurationRuleSet>().MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;
        var utcNow = timeProvider.GetUtcNow();
        var entity = DurationRuleSet.Create(Guid.NewGuid(), nextVersionNo, command.ValidFrom ?? utcNow, command.ValidTo, utcNow);

        dbContext.Set<DurationRuleSet>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DurationRuleSetView(entity.Id, entity.VersionNo, entity.Status, entity.ValidFrom, entity.ValidTo, entity.CreatedAt, entity.PublishedAt, []);
    }
}
