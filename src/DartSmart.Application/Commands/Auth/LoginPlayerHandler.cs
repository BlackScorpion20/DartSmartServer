using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;

namespace DartSmart.Application.Commands.Auth;

public sealed class LoginPlayerHandler : IRequestHandler<LoginPlayerCommand, Result<AuthResultDto>>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public LoginPlayerHandler(
        IPlayerRepository playerRepository, 
        IPasswordHasher passwordHasher, 
        IJwtService jwtService)
    {
        _playerRepository = playerRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResultDto>> Handle(LoginPlayerCommand request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByEmailAsync(request.Email, cancellationToken);
        
        if (player is null)
            return Result<AuthResultDto>.Failure("Invalid email or password");

        if (!_passwordHasher.Verify(request.Password, player.PasswordHash))
            return Result<AuthResultDto>.Failure("Invalid email or password");

        var accessToken = _jwtService.GenerateAccessToken(
            player.Id.Value.ToString(), 
            player.Email, 
            player.Username);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var playerDto = new PlayerDto(
            player.Id.Value.ToString(),
            player.Username,
            player.Email,
            player.CreatedAt,
            new PlayerStatisticsDto(
                player.Statistics.TotalGames,
                player.Statistics.Wins,
                player.Statistics.Best3DartScore,
                player.Statistics.Count180s,
                player.Statistics.HighestCheckout,
                player.Statistics.TotalDarts,
                player.Statistics.TotalPoints,
                player.Statistics.WinRate,
                player.Statistics.AveragePerDart,
                player.Statistics.Average3Dart
            )
        );

        return Result<AuthResultDto>.Success(new AuthResultDto(accessToken, refreshToken, playerDto));
    }
}
