namespace Tailbook.Modules.Identity.Api.Contracts.Abstractions;

public interface IUserReferenceValidationService
{
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken);
}
