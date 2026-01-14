using System.Security.Claims;

namespace DartSmartNet.Server.Infrastructure.Authentication;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string username, string email, string role);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiration();
}
