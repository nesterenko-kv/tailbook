using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Catalog.Application;

public interface ICatalogAccessPolicy
{
    bool CanReadCatalog(ICurrentUser currentUser);
    bool CanWriteCatalog(ICurrentUser currentUser);
}
