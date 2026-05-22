using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class CreateCatalogOfferCommandHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateCatalogOfferCommand, ErrorOr<OfferDetailView>>
{
    public async Task<ErrorOr<OfferDetailView>> ExecuteAsync(CreateCatalogOfferCommand command, CancellationToken cancellationToken)
    {
        var offer = CommercialOffer.Create(Guid.NewGuid(), command.Code, command.OfferType, command.DisplayName, timeProvider.GetUtcNow());
        if (offer.IsError)
        {
            return offer.Errors;
        }

        var duplicate = await dbContext.Set<CommercialOffer>()
            .AnyAsync(x => x.Code == offer.Value.Code, cancellationToken);
        if (duplicate)
        {
            return Error.Conflict("Catalog.OfferCodeExists", $"An offer with code '{offer.Value.Code}' already exists.");
        }

        dbContext.Set<CommercialOffer>().Add(offer.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OfferDetailView(
            offer.Value.Id,
            offer.Value.Code,
            offer.Value.OfferType,
            offer.Value.DisplayName,
            offer.Value.IsActive,
            [],
            offer.Value.CreatedAt,
            offer.Value.UpdatedAt);
    }
}
