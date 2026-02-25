using System.Security.Claims;

namespace Bangkok.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    (string Token, DateTime ExpiresAtUtc) GenerateRefreshToken();
    ClaimsPrincipal? ValidateAccessToken(string token);
}
