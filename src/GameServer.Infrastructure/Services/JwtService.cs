using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameServer.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GameServer.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiry;

    public JwtService(IConfiguration cfg)
    {
        _secret = cfg["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret is missing!");
        _issuer = cfg["Jwt:Issuer"] ?? "GameServer";
        _audience = cfg["Jwt:Audience"] ?? "GameServerClient";
        _expiry = int.Parse(cfg["Jwt:ExpiryMinutes"] ?? "60");
    }

    public string GenerateAccessToken(Guid userId, string username, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            _issuer, _audience, claims,
            expires: DateTime.UtcNow.AddMinutes(_expiry),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch { return null; }
    }

    public bool ValidateToken(string token)
    {
        try
        {
            new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
                ValidateIssuer = true, ValidIssuer = _issuer,
                ValidateAudience = true, ValidAudience = _audience,
                ClockSkew = TimeSpan.Zero
            }, out _);
            return true;
        }
        catch { return false; }
    }
}
