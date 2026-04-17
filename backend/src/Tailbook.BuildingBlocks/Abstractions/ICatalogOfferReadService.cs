namespace Tailbook.BuildingBlocks.Abstractions;

public interface ICatalogOfferReadService
{
    Task<IReadOnlyCollection<CatalogOfferSummary>> ListActiveOffersAsync(CancellationToken cancellationToken);
}

public sealed record CatalogOfferSummary(Guid Id, string Code, string OfferType, string DisplayName);
