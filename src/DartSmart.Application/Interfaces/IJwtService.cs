namespace DartSmart.Application.Interfaces;

/// <summary>
/// JWT token service interface
/// </summary>
public interface IJwtService
{
    string GenerateAccessToken(string playerId, string email, string username);
    string GenerateRefreshToken();
    (bool isValid, string? playerId) ValidateToken(string token);
}
