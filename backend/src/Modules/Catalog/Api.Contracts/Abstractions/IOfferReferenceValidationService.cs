namespace Tailbook.Modules.Catalog.Api.Contracts.Abstractions;

public interface IOfferReferenceValidationService
{
    Task<bool> ExistsAsync(Guid offerId, CancellationToken cancellationToken);
}
