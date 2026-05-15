using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class CreateCatalogPriceRuleSetCommandHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateCatalogPriceRuleSetCommand, PriceRuleSetView>
{
    public async Task<PriceRuleSetView> ExecuteAsync(CreateCatalogPriceRuleSetCommand command, CancellationToken cancellationToken)
    {
        var nextVersionNo = (await dbContext.Set<PriceRuleSet>().MaxAsync(x => (int?)x.VersionNo, cancellationToken) ?? 0) + 1;
        var utcNow = timeProvider.GetUtcNow();
        var entity = PriceRuleSet.Create(Guid.NewGuid(), nextVersionNo, command.ValidFrom ?? utcNow, command.ValidTo, utcNow);

        dbContext.Set<PriceRuleSet>().Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new PriceRuleSetView(entity.Id, entity.VersionNo, entity.Status, entity.ValidFrom, entity.ValidTo, entity.CreatedAt, entity.PublishedAt, []);
    }
}
