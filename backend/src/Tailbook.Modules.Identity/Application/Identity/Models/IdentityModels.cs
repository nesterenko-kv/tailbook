namespace Tailbook.Modules.Identity.Application.Identity.Models;

public sealed record LoginResult(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc, AuthenticatedUserView User);
public sealed record AuthenticatedUserView(Guid Id, string SubjectId, string Email, string DisplayName, string Status, Guid? ClientId, Guid? ContactPersonId, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
public sealed record RoleView(Guid Id, string Code, string DisplayName, IReadOnlyCollection<string> PermissionCodes);
public sealed record PermissionView(Guid Id, string Code, string DisplayName);
public sealed record UserSummaryView(Guid Id, string SubjectId, string Email, string DisplayName, string Status, IReadOnlyCollection<string> Roles, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record UserDetailView(Guid Id, string SubjectId, string Email, string DisplayName, string Status, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
