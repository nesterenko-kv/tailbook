namespace Tailbook.BuildingBlocks.Abstractions;

public interface IVisitCatalogReadService
{
    Task<IReadOnlyCollection<OfferExecutionComponentInfo>> GetIncludedComponentsAsync(Guid offerVersionId, CancellationToken cancellationToken);
    Task<OfferExecutionComponentInfo?> GetComponentAsync(Guid offerVersionComponentId, CancellationToken cancellationToken);
    Task<ProcedureReadModel?> GetProcedureAsync(Guid procedureId, CancellationToken cancellationToken);
}

public sealed record OfferExecutionComponentInfo(
    Guid Id,
    Guid OfferVersionId,
    Guid ProcedureId,
    string ProcedureCode,
    string ProcedureName,
    string ComponentRole,
    int SequenceNo,
    bool DefaultExpected);

public sealed record ProcedureReadModel(Guid Id, string Code, string Name, bool IsActive);
