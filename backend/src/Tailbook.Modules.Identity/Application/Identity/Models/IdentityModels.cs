namespace Tailbook.Modules.Identity.Application.Identity.Models;

public abstract record AuthenticationResult;
public sealed record AuthenticationSucceededResult(LoginResult Login) : AuthenticationResult;
public sealed record AuthenticationMfaRequiredResult(Guid ChallengeId, string FactorType, DateTimeOffset ExpiresAt) : AuthenticationResult;
public sealed record LoginResult(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, AuthenticatedUserView User);
public sealed record RoleView(Guid Id, string Code, string DisplayName, IReadOnlyCollection<string> PermissionCodes);
public sealed record PermissionView(Guid Id, string Code, string DisplayName);
public sealed record UserSummaryView(Guid Id, string SubjectId, string Email, string DisplayName, string Status, IReadOnlyCollection<string> Roles, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record UserDetailView(Guid Id, string SubjectId, string Email, string DisplayName, string Status, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
