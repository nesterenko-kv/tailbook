using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Pets.Application;

public interface IPetsAccessPolicy
{
    bool CanReadPets(ICurrentUser currentUser);
    bool CanWritePets(ICurrentUser currentUser);
    bool CanReadCatalog(ICurrentUser currentUser);
    bool CanReadContactData(ICurrentUser currentUser);
}
