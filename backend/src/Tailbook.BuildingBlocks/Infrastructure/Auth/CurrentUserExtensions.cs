namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public static class CurrentUserExtensions
{
    public static bool HasRole(this ICurrentUser currentUser, string roleCode)
    {
        return currentUser.Roles.Any(x => string.Equals(x, roleCode, StringComparison.OrdinalIgnoreCase));
    }

    public static bool HasAnyRole(this ICurrentUser currentUser, params string[] roleCodes)
    {
        var set = new HashSet<string>(roleCodes, StringComparer.OrdinalIgnoreCase);
        return currentUser.Roles.Any(set.Contains);
    }

    public static bool HasPermission(this ICurrentUser currentUser, string permissionCode)
    {
        return currentUser.Permissions.Any(x => string.Equals(x, permissionCode, StringComparison.OrdinalIgnoreCase));
    }
}
