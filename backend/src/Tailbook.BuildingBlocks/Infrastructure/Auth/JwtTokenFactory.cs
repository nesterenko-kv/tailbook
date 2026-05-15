using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public sealed class JwtTokenFactory(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    public GeneratedToken CreateToken(string userId, string subjectId, string email, string displayName, Guid? clientId, Guid? contactPersonId, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var settings = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(settings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subjectId),
            new(ClaimTypes.Email, email),
            new(TailbookClaimTypes.DisplayName, displayName),
            new(TailbookClaimTypes.UserId, userId)
        };

        if (clientId.HasValue)
            claims.Add(new Claim(TailbookClaimTypes.ClientId, clientId.Value.ToString("D")));
        if (contactPersonId.HasValue)
            claims.Add(new Claim(TailbookClaimTypes.ContactPersonId, contactPersonId.Value.ToString("D")));

        claims.AddRange(roles.Distinct(StringComparer.OrdinalIgnoreCase).Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Distinct(StringComparer.OrdinalIgnoreCase).Select(permission => new Claim(TailbookClaimTypes.Permission, permission)));

        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return new GeneratedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

public sealed record GeneratedToken(string AccessToken, DateTimeOffset ExpiresAt);
