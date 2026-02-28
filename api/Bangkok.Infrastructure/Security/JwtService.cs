using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Bangkok.Application.Configuration;
using Bangkok.Application.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bangkok.Infrastructure.Security;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();
    private readonly SymmetricSecurityKey _signingKey;

    public JwtService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
        if (string.IsNullOrWhiteSpace(_settings.SigningKey) || _settings.SigningKey.Length < 32)
            throw new InvalidOperationException("JWT SigningKey must be at least 32 characters.");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
    }

    public string GenerateAccessToken(Guid userId, string email, IReadOnlyList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        foreach (var role in roles)
        {
            if (!string.IsNullOrEmpty(role))
                claims.Add(new Claim(ClaimTypes.Role, role));
        }
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes);
        var token = new JwtSecurityToken(
            _settings.Issuer,
            _settings.Audience,
            claims,
            expires: expires,
            signingCredentials: credentials);
        return _tokenHandler.WriteToken(token);
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        var token = Convert.ToBase64String(randomBytes);
        var expiresAtUtc = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);
        return (token, expiresAtUtc);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            return _tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }
}
