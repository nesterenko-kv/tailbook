using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public sealed class JwtTokenFactory(IOptions<JwtOptions> options)
{
    public string CreateToken(string subjectId, string email, IEnumerable<string> roles)
    {
        var settings = options.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, subjectId),
            new(ClaimTypes.Email, email)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(settings.ExpirationMinutes),
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
