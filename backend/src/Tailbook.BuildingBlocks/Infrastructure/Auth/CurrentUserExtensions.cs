namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public static class CurrentUserExtensions
{
    public static bool HasPermission(this ICurrentUser currentUser, string permissionCode)
    {
        return currentUser.Permissions.Any(x => string.Equals(x, permissionCode, StringComparison.OrdinalIgnoreCase));
    }
}
