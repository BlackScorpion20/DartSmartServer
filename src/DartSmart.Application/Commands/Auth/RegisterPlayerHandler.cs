using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Entities;

namespace DartSmart.Application.Commands.Auth;

public sealed class RegisterPlayerHandler : IRequestHandler<RegisterPlayerCommand, Result<AuthResultDto>>
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public RegisterPlayerHandler(
        IPlayerRepository playerRepository, 
        IPasswordHasher passwordHasher, 
        IJwtService jwtService)
    {
        _playerRepository = playerRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Result<AuthResultDto>> Handle(RegisterPlayerCommand request, CancellationToken cancellationToken)
    {
        if (await _playerRepository.EmailExistsAsync(request.Email, cancellationToken))
            return Result<AuthResultDto>.Failure("Email already registered");

        if (await _playerRepository.UsernameExistsAsync(request.Username, cancellationToken))
            return Result<AuthResultDto>.Failure("Username already taken");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var player = Player.Create(request.Username, request.Email, passwordHash);

        await _playerRepository.AddAsync(player, cancellationToken);

        var accessToken = _jwtService.GenerateAccessToken(
            player.Id.Value.ToString(), 
            player.Email, 
            player.Username);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var playerDto = MapToDto(player);
        return Result<AuthResultDto>.Success(new AuthResultDto(accessToken, refreshToken, playerDto));
    }

    private static PlayerDto MapToDto(Player player) => new(
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
}
