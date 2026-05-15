namespace Tailbook.BuildingBlocks.Abstractions;

public interface IPetReferenceValidationService
{
    Task<bool> ExistsAsync(Guid petId, CancellationToken cancellationToken);
}
