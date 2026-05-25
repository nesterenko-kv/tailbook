using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services;

public sealed class CatalogReferenceServices(AppDbContext dbContext) : IOfferReferenceValidationService
{
    public async Task<bool> ExistsAsync(Guid offerId, CancellationToken cancellationToken) => await dbContext.Set<CommercialOffer>().AnyAsync(x => x.Id == offerId, cancellationToken);
}
