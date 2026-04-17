namespace Tailbook.BuildingBlocks.Abstractions;

public interface IClientOnboardingService
{
    Task<ClientOnboardingResult> CreateClientPortalProfileAsync(CreateClientPortalProfileCommand command, CancellationToken cancellationToken);
}

public sealed record CreateClientPortalProfileCommand(
    string DisplayName,
    string FirstName,
    string? LastName,
    string Email,
    string? Phone,
    string? Instagram);

public sealed record ClientOnboardingResult(Guid ClientId, Guid ContactPersonId);
