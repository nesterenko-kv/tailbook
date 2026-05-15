namespace Tailbook.BuildingBlocks.Abstractions;

public interface IContactReferenceValidationService
{
    Task<bool> ExistsAsync(Guid contactId, CancellationToken cancellationToken);
}
