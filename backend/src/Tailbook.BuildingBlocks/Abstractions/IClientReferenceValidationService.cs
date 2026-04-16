namespace Tailbook.BuildingBlocks.Abstractions;

public interface IClientReferenceValidationService
{
    Task<bool> ExistsAsync(Guid clientId, CancellationToken cancellationToken);
}
