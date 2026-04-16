using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(TailbookClaimTypes.UserId);

    public string? SubjectId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public IReadOnlyCollection<string> Roles =>
        httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray()
        ?? Array.Empty<string>();

    public IReadOnlyCollection<string> Permissions =>
        httpContextAccessor.HttpContext?.User.FindAll(TailbookClaimTypes.Permission).Select(x => x.Value).ToArray()
        ?? Array.Empty<string>();
}
