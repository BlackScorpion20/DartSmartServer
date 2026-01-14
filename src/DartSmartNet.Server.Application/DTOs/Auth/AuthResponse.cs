namespace DartSmartNet.Server.Application.DTOs.Auth;

public sealed record AuthResponse(
    Guid UserId,
    string Username,
    string Email,
    string Token,
    string RefreshToken,
    int ExpiresIn
);
