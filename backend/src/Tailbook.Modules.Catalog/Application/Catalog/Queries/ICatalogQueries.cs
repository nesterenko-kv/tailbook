using ErrorOr;

namespace Tailbook.Modules.Catalog.Application.Catalog.Queries;

public interface ICatalogQueries
{
    Task<IReadOnlyCollection<ProcedureView>> ListProceduresAsync(CancellationToken cancellationToken);
    Task<ErrorOr<ProcedureView>> CreateProcedureAsync(string code, string name, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OfferListItemView>> ListOffersAsync(CancellationToken cancellationToken);
    Task<OfferDetailView?> GetOfferAsync(Guid offerId, CancellationToken cancellationToken);
    Task<ErrorOr<OfferDetailView>> CreateOfferAsync(string code, string offerType, string displayName, CancellationToken cancellationToken);
    Task<OfferVersionView?> CreateOfferVersionAsync(Guid offerId, DateTime? validFromUtc, DateTime? validToUtc, string? policyText, string? changeNote, CancellationToken cancellationToken);
    Task<ErrorOr<OfferVersionComponentView>> AddComponentAsync(Guid versionId, Guid procedureId, string componentRole, int sequenceNo, bool defaultExpected, CancellationToken cancellationToken);
    Task<ErrorOr<OfferVersionView>> PublishOfferVersionAsync(Guid versionId, CancellationToken cancellationToken);
}
