namespace Tailbook.BuildingBlocks.Abstractions;

public interface IClientOnboardingService
{
    Task<ClientOnboardingResult> CreateClientPortalProfileAsync(CreateClientPortalProfileInput command, CancellationToken cancellationToken);
}

public sealed record CreateClientPortalProfileInput(
    string DisplayName,
    string FirstName,
    string? LastName,
    string Email,
    string? Phone,
    string? Instagram);

public sealed record ClientOnboardingResult(Guid ClientId, Guid ContactPersonId);
