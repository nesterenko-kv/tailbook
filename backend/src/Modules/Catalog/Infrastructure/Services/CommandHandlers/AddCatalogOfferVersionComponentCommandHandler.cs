using ErrorOr;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Catalog.Infrastructure.Services.CommandHandlers;

public sealed class AddCatalogOfferVersionComponentCommandHandler(AppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<AddCatalogOfferVersionComponentCommand, ErrorOr<OfferVersionComponentView>>
{
    public async Task<ErrorOr<OfferVersionComponentView>> ExecuteAsync(AddCatalogOfferVersionComponentCommand command, CancellationToken cancellationToken)
    {
        var offer = await LoadOfferAggregateByVersionIdAsync(command.VersionId, cancellationToken);
        if (offer.IsError)
        {
            return offer.Errors;
        }

        var canAdd = offer.Value.EnsureVersionCanAcceptComponent(command.VersionId);
        if (canAdd.IsError)
        {
            return canAdd.Errors;
        }

        var normalizedRole = OfferVersionComponent.NormalizeRole(command.ComponentRole);
        if (normalizedRole.IsError)
        {
            return normalizedRole.Errors;
        }

        var procedure = await dbContext.Set<ProcedureCatalogItem>().SingleOrDefaultAsync(x => x.Id == command.ProcedureId, cancellationToken);
        if (procedure is null)
        {
            return Error.NotFound("Catalog.ProcedureNotFound", "Procedure does not exist.");
        }

        var entity = offer.Value.AddComponent(
            command.VersionId,
            command.ProcedureId,
            normalizedRole.Value,
            command.SequenceNo,
            command.DefaultExpected,
            timeProvider.GetUtcNow());
        if (entity.IsError)
        {
            return entity.Errors;
        }

        dbContext.Set<OfferVersionComponent>().Add(entity.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new OfferVersionComponentView(
            entity.Value.Id,
            entity.Value.OfferVersionId,
            entity.Value.ProcedureId,
            procedure.Code,
            procedure.Name,
            entity.Value.ComponentRole,
            entity.Value.SequenceNo,
            entity.Value.DefaultExpected,
            entity.Value.CreatedAt);
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
