using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class PublishCatalogOfferVersionCommandHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<PublishCatalogOfferVersionCommand, ErrorOr<OfferVersionView>>
{
    public async Task<ErrorOr<OfferVersionView>> ExecuteAsync(PublishCatalogOfferVersionCommand command, CancellationToken cancellationToken)
    {
        var offer = await LoadOfferAggregateByVersionIdAsync(command.VersionId, cancellationToken);
        if (offer.IsError)
        {
            return offer.Errors;
        }

        var publish = offer.Value.PublishVersion(command.VersionId, timeProvider.GetUtcNow());
        if (publish.IsError)
        {
            return publish.Errors;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var version = offer.Value.Versions.Single(x => x.Id == command.VersionId);
        var components = version.Components.OrderBy(x => x.SequenceNo).ToArray();
        var procedureIds = components.Select(x => x.ProcedureId).Distinct().ToArray();
        var procedures = await dbContext.Set<ProcedureCatalogItem>()
            .Where(x => procedureIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

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
            components.Select(x => new OfferVersionComponentView(
                x.Id,
                x.OfferVersionId,
                x.ProcedureId,
                procedures[x.ProcedureId].Code,
                procedures[x.ProcedureId].Name,
                x.ComponentRole,
                x.SequenceNo,
                x.DefaultExpected,
                x.CreatedAt)).ToArray());
    }

    private async Task<ErrorOr<CommercialOffer>> LoadOfferAggregateByVersionIdAsync(Guid versionId, CancellationToken cancellationToken)
    {
        var offerId = await dbContext.Set<OfferVersion>()
            .Where(x => x.Id == versionId)
            .Select(x => (Guid?)x.OfferId)
            .SingleOrDefaultAsync(cancellationToken);
        if (offerId is null)
        {
            return CatalogErrors.OfferVersionNotFound;
        }

        var offer = await dbContext.Set<CommercialOffer>()
            .Include(x => x.Versions)
            .ThenInclude(x => x.Components)
            .SingleOrDefaultAsync(x => x.Id == offerId.Value, cancellationToken);

        return offer is null ? CatalogErrors.OfferVersionNotFound : offer;
    }
}
