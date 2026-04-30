namespace Tailbook.Modules.Identity.Application.Identity.Models;

public sealed record IssuedRefreshToken(string Token, DateTime ExpiresAtUtc);
