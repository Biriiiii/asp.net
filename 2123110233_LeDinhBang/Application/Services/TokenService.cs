using BookStore.Application.Interfaces;
using BookStore.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BookStore.Application.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public string GenerateAccessToken(User user, IEnumerable<string> roles)
    {
        // 1. Lấy Key an toàn (Nếu null thì lấy chuỗi dự phòng dài để không lỗi)
        var secretKey = _config["Jwt:Key"] ?? "b8f9a2c4d6e8b1a3f5c7d9e0b2a4c6e8f0a1b3c5d7e9f1a2b4c6";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 2. Parse thời gian hết hạn an toàn
        if (!int.TryParse(_config["Jwt:ExpiresInMinutes"], out int expiresMinutes))
        {
            expiresMinutes = 60; // Mặc định 60 phút nếu config lỗi
        }
        var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Name,  user.FullName),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("phone", user.Phone ?? ""),
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "BookStoreAPI",
            audience: _config["Jwt:Audience"] ?? "BookStoreClient",
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var secretKey = _config["Jwt:Key"] ?? "b8f9a2c4d6e8b1a3f5c7d9e0b2a4c6e8f0a1b3c5d7e9f1a2b4c6";
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"] ?? "BookStoreAPI",
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"] ?? "BookStoreClient",
                ValidateLifetime = false
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }
}