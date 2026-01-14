namespace DartSmartNet.Server.Application.DTOs.Auth;

public sealed record LoginRequest(
    string Username,
    string Password
);
