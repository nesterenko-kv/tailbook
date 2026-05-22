namespace Tailbook.Modules.Customer.Api.Contracts.Abstractions;

public interface IContactReferenceValidationService
{
    Task<bool> ExistsAsync(Guid contactId, CancellationToken cancellationToken);
}
