using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MiniErp.Api.IntegrationTests;

public static class TestTokens
{
    public static string Create(
        string[] roles,
        string? secret = null,
        DateTime? expiresAtUtc = null)
    {
        var expires = expiresAtUtc ?? DateTime.UtcNow.AddMinutes(15);

        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("email", "test@minierp.local"),
            new("name", "Test User")
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret ?? ApiFactory.SecretKey));

        var token = new JwtSecurityToken(
            issuer: ApiFactory.Issuer,
            audience: ApiFactory.Audience,
            claims: claims,
            notBefore: expires.AddMinutes(-30),
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}