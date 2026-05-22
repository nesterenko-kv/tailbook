using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services;

public sealed class CatalogReadService(AppDbContext dbContext) : ICatalogReadService
{
    public async Task<IReadOnlyCollection<ProcedureView>> ListProceduresAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Set<ProcedureCatalogItem>()
            .OrderBy(x => x.Name)
            .Select(x => new ProcedureView(x.Id, x.Code, x.Name, x.IsActive, x.CreatedAt, x.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OfferListItemView>> ListOffersAsync(CancellationToken cancellationToken)
    {
        var offers = await dbContext.Set<CommercialOffer>()
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        var offerIds = offers.Select(x => x.Id).ToArray();
        var versions = await dbContext.Set<OfferVersion>()
            .Where(x => offerIds.Contains(x.OfferId))
            .ToListAsync(cancellationToken);

        return offers.Select(x => new OfferListItemView(
            x.Id,
            x.Code,
            x.OfferType,
            x.DisplayName,
            x.IsActive,
            versions.Count(v => v.OfferId == x.Id),
            versions.Any(v => v.OfferId == x.Id && v.Status == OfferVersionStatusCodes.Published),
            x.CreatedAt,
            x.UpdatedAt)).ToArray();
    }

    public async Task<OfferDetailView?> GetOfferAsync(Guid offerId, CancellationToken cancellationToken)
    {
        var offer = await dbContext.Set<CommercialOffer>().SingleOrDefaultAsync(x => x.Id == offerId, cancellationToken);
        if (offer is null)
        {
            return null;
        }

        var versions = await dbContext.Set<OfferVersion>()
            .Where(x => x.OfferId == offerId)
            .OrderByDescending(x => x.VersionNo)
            .ToListAsync(cancellationToken);

        var versionIds = versions.Select(x => x.Id).ToArray();
        var components = await dbContext.Set<OfferVersionComponent>()
            .Where(x => versionIds.Contains(x.OfferVersionId))
            .OrderBy(x => x.SequenceNo)
            .ToListAsync(cancellationToken);

        var procedureIds = components.Select(x => x.ProcedureId).Distinct().ToArray();
        var procedures = await dbContext.Set<ProcedureCatalogItem>()
            .Where(x => procedureIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return new OfferDetailView(
            offer.Id,
            offer.Code,
            offer.OfferType,
            offer.DisplayName,
            offer.IsActive,
            versions.Select(version => new OfferVersionView(
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
                components.Where(component => component.OfferVersionId == version.Id)
                    .Select(component =>
                    {
                        var procedure = procedures[component.ProcedureId];
                        return new OfferVersionComponentView(
                            component.Id,
                            component.OfferVersionId,
                            component.ProcedureId,
                            procedure.Code,
                            procedure.Name,
                            component.ComponentRole,
                            component.SequenceNo,
                            component.DefaultExpected,
                            component.CreatedAt);
                    })
                    .ToArray()))
                .ToArray(),
            offer.CreatedAt,
            offer.UpdatedAt);
    }
}
