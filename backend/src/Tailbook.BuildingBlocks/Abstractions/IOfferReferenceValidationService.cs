namespace Tailbook.BuildingBlocks.Abstractions;

public interface IOfferReferenceValidationService
{
    Task<bool> ExistsAsync(Guid offerId, CancellationToken cancellationToken);
}
