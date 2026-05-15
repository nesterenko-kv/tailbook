namespace Tailbook.BuildingBlocks.Abstractions;

public interface IUserReferenceValidationService
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);
}
