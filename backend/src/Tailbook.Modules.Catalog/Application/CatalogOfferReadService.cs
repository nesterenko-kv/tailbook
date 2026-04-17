using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Domain;

namespace Tailbook.Modules.Catalog.Application;

public sealed class CatalogOfferReadService(AppDbContext dbContext) : ICatalogOfferReadService
{
    public async Task<IReadOnlyCollection<CatalogOfferSummary>> ListActiveOffersAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Set<CommercialOffer>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayName)
            .Select(x => new CatalogOfferSummary(x.Id, x.Code, x.OfferType, x.DisplayName))
            .ToListAsync(cancellationToken);
    }
}
