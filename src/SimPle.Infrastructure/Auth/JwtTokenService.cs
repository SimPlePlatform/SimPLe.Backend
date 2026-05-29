using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SimPle.Application.Common.Interfaces;

namespace SimPle.Infrastructure.Auth;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public (string Token, DateTime ExpiresAt, string Jti) GenerateAccessToken(Guid userId, string username, string role, Guid familyId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);
        var jti = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            // sid = session family ID, stable across token rotation, used for immediate session revocation.
            new Claim(JwtRegisteredClaimNames.Sid, familyId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt, jti);
    }

    public string GenerateRawRefreshToken()
    {
        // 32 bytes = 256 bits of randomness. Enough entropy that brute-force is not feasible.
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    public string HashToken(string rawToken)
    {
        // SHA-256 is fine here because the input (raw refresh token) is already 256 bits of
        // random data — there's no dictionary attack surface as there is with passwords.
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    public Guid? ValidateAccessToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));

        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            }, out var validated);

            var jwt = (JwtSecurityToken)validated;
            return Guid.Parse(jwt.Subject);
        }
        catch
        {
            return null;
        }
    }
}
