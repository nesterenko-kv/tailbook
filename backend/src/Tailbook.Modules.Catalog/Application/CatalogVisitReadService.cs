using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Catalog.Contracts;
using Tailbook.Modules.Catalog.Domain;

namespace Tailbook.Modules.Catalog.Application;

public sealed class CatalogVisitReadService(AppDbContext dbContext) : IVisitCatalogReadService
{
    public async Task<IReadOnlyCollection<OfferExecutionComponentInfo>> GetIncludedComponentsAsync(Guid offerVersionId, CancellationToken cancellationToken)
    {
        var components = await dbContext.Set<OfferVersionComponent>()
            .Where(x => x.OfferVersionId == offerVersionId && x.ComponentRole == OfferComponentRoleCodes.Included)
            .OrderBy(x => x.SequenceNo)
            .ToListAsync(cancellationToken);

        var procedureIds = components.Select(x => x.ProcedureId).Distinct().ToArray();
        var procedures = await dbContext.Set<ProcedureCatalogItem>()
            .Where(x => procedureIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return components.Select(x =>
        {
            var procedure = procedures[x.ProcedureId];
            return new OfferExecutionComponentInfo(
                x.Id,
                x.OfferVersionId,
                x.ProcedureId,
                procedure.Code,
                procedure.Name,
                x.ComponentRole,
                x.SequenceNo,
                x.DefaultExpected);
        }).ToArray();
    }

    public async Task<OfferExecutionComponentInfo?> GetComponentAsync(Guid offerVersionComponentId, CancellationToken cancellationToken)
    {
        var component = await dbContext.Set<OfferVersionComponent>()
            .SingleOrDefaultAsync(x => x.Id == offerVersionComponentId, cancellationToken);

        if (component is null)
        {
            return null;
        }

        var procedure = await dbContext.Set<ProcedureCatalogItem>()
            .SingleAsync(x => x.Id == component.ProcedureId, cancellationToken);

        return new OfferExecutionComponentInfo(
            component.Id,
            component.OfferVersionId,
            component.ProcedureId,
            procedure.Code,
            procedure.Name,
            component.ComponentRole,
            component.SequenceNo,
            component.DefaultExpected);
    }

    public async Task<ProcedureReadModel?> GetProcedureAsync(Guid procedureId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<ProcedureCatalogItem>()
            .Where(x => x.Id == procedureId)
            .Select(x => new ProcedureReadModel(x.Id, x.Code, x.Name, x.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
