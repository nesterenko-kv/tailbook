using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Domain;

namespace Tailbook.Modules.Catalog.Application;

public sealed class CatalogReferenceServices(AppDbContext dbContext) : IOfferReferenceValidationService
{
    public async Task<bool> ExistsAsync(Guid offerId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<CommercialOffer>().AnyAsync(x => x.Id == offerId, cancellationToken);
    }
}
