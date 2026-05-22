namespace Tailbook.Modules.Catalog.Application.Catalog.Queries;

public interface ICatalogReadService
{
    Task<IReadOnlyCollection<ProcedureView>> ListProceduresAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OfferListItemView>> ListOffersAsync(CancellationToken cancellationToken);
    Task<OfferDetailView?> GetOfferAsync(Guid offerId, CancellationToken cancellationToken);
}
