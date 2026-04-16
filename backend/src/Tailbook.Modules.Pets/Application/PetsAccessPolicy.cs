using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Pets.Application;

public sealed class PetsAccessPolicy : IPetsAccessPolicy
{
    private const string PetsReadPermission = "pets.read";
    private const string PetsWritePermission = "pets.write";
    private const string PetsCatalogReadPermission = "pets.catalog.read";
    private const string ContactsReadPermission = "crm.contacts.read";

    public bool CanReadPets(ICurrentUser currentUser) => currentUser.HasPermission(PetsReadPermission);
    public bool CanWritePets(ICurrentUser currentUser) => currentUser.HasPermission(PetsWritePermission);
    public bool CanReadCatalog(ICurrentUser currentUser) => currentUser.HasPermission(PetsCatalogReadPermission) || currentUser.HasPermission(PetsReadPermission);
    public bool CanReadContactData(ICurrentUser currentUser) => currentUser.HasPermission(ContactsReadPermission);
}
