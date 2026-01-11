namespace DartSmart.Application.DTOs;

public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    PlayerDto Player
);

public record LoginDto(
    string Email,
    string Password
);

public record RegisterDto(
    string Username,
    string Email,
    string Password
);
