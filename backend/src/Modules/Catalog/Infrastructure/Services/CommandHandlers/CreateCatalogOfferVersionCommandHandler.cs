using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class CreateCatalogOfferVersionCommandHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateCatalogOfferVersionCommand, OfferVersionView?>
{
    public async Task<OfferVersionView?> ExecuteAsync(CreateCatalogOfferVersionCommand command, CancellationToken cancellationToken)
    {
        var offer = await LoadOfferAggregateAsync(command.OfferId, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        var version = offer.CreateVersion(
            Guid.NewGuid(),
            command.ValidFrom,
            command.ValidTo,
            command.PolicyText,
            command.ChangeNote,
            timeProvider.GetUtcNow()).Value;

        dbContext.Set<OfferVersion>().Add(version);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OfferVersionView(
            version.Id,
            version.OfferId,
            version.VersionNo,
            version.Status,
            version.ValidFrom,
            version.ValidTo,
            version.PolicyText,
            version.ChangeNote,
            version.CreatedAt,
            version.PublishedAt,
            []);
    }

    private async Task<CommercialOffer?> LoadOfferAggregateAsync(Guid offerId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<CommercialOffer>()
            .Include(x => x.Versions)
            .ThenInclude(x => x.Components)
            .SingleOrDefaultAsync(x => x.Id == offerId, cancellationToken);
    }
}
