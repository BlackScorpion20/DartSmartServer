using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Application.Interfaces;
using DartSmart.Domain.Entities;

namespace DartSmart.Application.Commands.Game;

public sealed class CreateBotHandler : IRequestHandler<CreateBotCommand, Result<PlayerDto>>
{
    private readonly IPlayerRepository _playerRepository;

    public CreateBotHandler(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<Result<PlayerDto>> Handle(CreateBotCommand request, CancellationToken cancellationToken)
    {
        // Check if username/bot exists
        // Create bot
        // Save
        
        try
        {
            var bot = Player.CreateBot(request.Username, request.SkillLevel);
            
            // Check existence?
            // Simple check by email (we generated a fake email for bots)
            if (await _playerRepository.EmailExistsAsync(bot.Email, cancellationToken))
            {
                // Optionally handle duplicate bot names or just append ID
                // For now, let's fail if name taken to be simple
                return Result<PlayerDto>.Failure("Bot with this name already exists");
            }

            await _playerRepository.AddAsync(bot, cancellationToken);
            
            return Result<PlayerDto>.Success(new PlayerDto(
                bot.Id.Value.ToString(),
                bot.Username,
                bot.Email,
                bot.CreatedAt,
                new DTOs.PlayerStatisticsDto(
                    bot.Statistics.TotalGames,
                    bot.Statistics.Wins,
                    bot.Statistics.Best3DartScore,
                    bot.Statistics.Count180s,
                    bot.Statistics.HighestCheckout,
                    bot.Statistics.TotalDarts,
                    bot.Statistics.TotalPoints,
                    bot.Statistics.WinRate,
                    bot.Statistics.AveragePerDart,
                    bot.Statistics.Average3Dart
                )
            ));
        }
        catch (Exception ex)
        {
            return Result<PlayerDto>.Failure(ex.Message);
        }
    }
}
