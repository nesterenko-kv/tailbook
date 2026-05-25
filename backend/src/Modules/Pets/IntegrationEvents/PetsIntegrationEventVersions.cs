using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Pets.IntegrationEvents;

public static class PetsIntegrationEventVersions
{
    public const int PetCreated = IntegrationEventVersionPolicy.InitialVersion;
    public const int PetUpdated = IntegrationEventVersionPolicy.InitialVersion;
}
