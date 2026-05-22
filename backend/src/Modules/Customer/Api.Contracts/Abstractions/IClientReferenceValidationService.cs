namespace Tailbook.Modules.Customer.Api.Contracts.Abstractions;

public interface IClientReferenceValidationService
{
    Task<bool> ExistsAsync(Guid clientId, CancellationToken cancellationToken);
}
