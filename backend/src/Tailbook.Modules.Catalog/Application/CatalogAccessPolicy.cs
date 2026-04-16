using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Catalog.Application;

public sealed class CatalogAccessPolicy : ICatalogAccessPolicy
{
    private const string CatalogReadPermission = "catalog.read";
    private const string CatalogWritePermission = "catalog.write";

    public bool CanReadCatalog(ICurrentUser currentUser) => currentUser.HasPermission(CatalogReadPermission);
    public bool CanWriteCatalog(ICurrentUser currentUser) => currentUser.HasPermission(CatalogWritePermission);
}
