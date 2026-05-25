namespace Tailbook.Modules.Pets.Api.Contracts.Abstractions;

public interface IPetReferenceValidationService
{
    Task<bool> ExistsAsync(Guid petId, CancellationToken cancellationToken);
}
