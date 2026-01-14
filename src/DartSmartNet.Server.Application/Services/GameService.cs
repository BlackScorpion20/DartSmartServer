using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStatisticsService _statisticsService;

    public GameService(
        IGameRepository gameRepository,
        IUserRepository userRepository,
        IStatisticsService statisticsService)
    {
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _statisticsService = statisticsService;
    }

    public async Task<GameStateDto> CreateGameAsync(
        GameType gameType,
        int? startingScore,
        Guid[] playerIds,
        bool isOnline,
        CancellationToken cancellationToken = default)
    {

        // Validate starting score for X01 games
        if (gameType == GameType.X01)
        {
            if (!startingScore.HasValue)
            {
                throw new InvalidOperationException("Starting score is required for X01 games");
            }
        }

        // Check if any player is a bot
        var isBotGame = playerIds.Any(id => id == Guid.Empty);

        // Create game session
        var game = GameSession.Create(gameType, startingScore, isOnline, isBotGame);

        // Add players with order
        for (int i = 0; i < playerIds.Length; i++)
        {
            game.AddPlayer(playerIds[i], i);
        }

        await _gameRepository.AddAsync(game, cancellationToken);


        return await MapToGameStateDto(game, cancellationToken);
    }

    public async Task<GameStateDto> GetGameStateAsync(Guid gameId, CancellationToken cancellationToken = default)
    {

        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

        if (game == null)
        {
            throw new InvalidOperationException($"Game {gameId} not found");
        }

        return await MapToGameStateDto(game, cancellationToken);
    }

    public async Task<GameStateDto> StartGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {

        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

        if (game == null)
        {
            throw new InvalidOperationException($"Game {gameId} not found");
        }

        if (game.Status != GameStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException($"Game cannot be started from status {game.Status}");
        }

        game.Start();
        await _gameRepository.UpdateAsync(game, cancellationToken);


        return await MapToGameStateDto(game, cancellationToken);
    }

    public async Task<GameStateDto> RegisterThrowAsync(
        Guid gameId,
        Guid userId,
        Score score,
        byte[]? rawData = null,
        CancellationToken cancellationToken = default)
    {

        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

        if (game == null)
        {
            throw new InvalidOperationException($"Game {gameId} not found");
        }

        if (game.Status != GameStatus.InProgress)
        {
            throw new InvalidOperationException($"Game is not in progress (status: {game.Status})");
        }

        var player = game.Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null)
        {
            throw new InvalidOperationException($"User {userId} is not a player in this game");
        }

        // Calculate round number based on total throws
        var playerThrows = game.Throws.Count(t => t.UserId == userId);
        var roundNumber = (playerThrows / 3) + 1;
        var dartNumber = (playerThrows % 3) + 1;

        // Create and add the throw
        var dartThrow = DartThrow.Create(gameId, userId, roundNumber, dartNumber, score, rawData);
        game.AddThrow(dartThrow);

        // Check for game completion (for X01 games)
        if (IsX01Game(game.GameType) && game.StartingScore.HasValue)
        {
            var currentScore = CalculateCurrentScore(game, userId);

            if (currentScore == 0 && score.Multiplier == Multiplier.Double)
            {
                // Player finished with a double - they win
                game.Complete(userId);
                player.SetFinalScore(0);


                // Update statistics
                await UpdateGameStatistics(game, cancellationToken);
            }
            else if (currentScore < 0 || (currentScore == 0 && score.Multiplier != Multiplier.Double))
            {
                // Bust - invalid throw, revert score for this round
                // Note: In a full implementation, we'd need to handle bust logic more carefully
            }
        }

        await _gameRepository.UpdateAsync(game, cancellationToken);

        return await MapToGameStateDto(game, cancellationToken);
    }

    public async Task<GameStateDto> EndGameAsync(Guid gameId, CancellationToken cancellationToken = default)
    {

        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

        if (game == null)
        {
            throw new InvalidOperationException($"Game {gameId} not found");
        }

        if (game.Status == GameStatus.Completed)
        {
            return await MapToGameStateDto(game, cancellationToken);
        }

        if (game.Status != GameStatus.InProgress)
        {
            game.Abandon();
        }
        else
        {
            // Determine winner based on current scores for X01 games
            if (IsX01Game(game.GameType) && game.StartingScore.HasValue)
            {
                var playerScores = game.Players.Select(p => new
                {
                    Player = p,
                    CurrentScore = CalculateCurrentScore(game, p.UserId)
                }).ToList();

                var winner = playerScores
                    .Where(ps => ps.CurrentScore >= 0)
                    .OrderBy(ps => ps.CurrentScore)
                    .FirstOrDefault();

                if (winner != null)
                {
                    game.Complete(winner.Player.UserId);
                    winner.Player.SetFinalScore(winner.CurrentScore);
                }
                else
                {
                    game.Abandon();
                }
            }
            else
            {
                // For other game types, just mark as abandoned if manually ended
                game.Abandon();
            }
        }

        await _gameRepository.UpdateAsync(game, cancellationToken);

        // Update statistics if game was completed properly
        if (game.Status == GameStatus.Completed)
        {
            await UpdateGameStatistics(game, cancellationToken);
        }


        return await MapToGameStateDto(game, cancellationToken);
    }

    public async Task<IEnumerable<GameStateDto>> GetUserGamesAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default)
    {

        var games = await _gameRepository.GetUserGamesAsync(userId, limit, cancellationToken);

        var gameDtos = new List<GameStateDto>();
        foreach (var game in games)
        {
            gameDtos.Add(await MapToGameStateDto(game, cancellationToken));
        }

        return gameDtos;
    }

    private async Task<GameStateDto> MapToGameStateDto(GameSession game, CancellationToken cancellationToken)
    {
        var players = new List<PlayerGameDto>();

        foreach (var player in game.Players.OrderBy(p => p.PlayerOrder))
        {
            var user = await _userRepository.GetByIdAsync(player.UserId, cancellationToken);

            players.Add(new PlayerGameDto(
                player.UserId,
                user?.Username ?? "Unknown",
                player.PlayerOrder,
                player.FinalScore,
                player.DartsThrown,
                player.PointsScored,
                player.PPD,
                player.IsWinner
            ));
        }

        // Calculate current player index based on total throws
        var currentPlayerIndex = 0;
        if (game.Status == GameStatus.InProgress && game.Players.Any())
        {
            var totalThrows = game.Throws.Count;
            var playersCount = game.Players.Count;
            currentPlayerIndex = (totalThrows / 3) % playersCount;
        }

        return new GameStateDto(
            game.Id,
            game.GameType,
            game.StartingScore,
            game.Status,
            game.StartedAt,
            game.EndedAt,
            game.WinnerId,
            game.IsOnline,
            game.IsBotGame,
            players,
            currentPlayerIndex
        );
    }

    private bool IsX01Game(GameType gameType)
    {
        return gameType == GameType.X01;
    }

    private int CalculateCurrentScore(GameSession game, Guid userId)
    {
        if (!game.StartingScore.HasValue)
            return 0;

        var remainingScore = game.StartingScore.Value;
        
        var playerThrows = game.Throws
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.RoundNumber)
            .ThenBy(t => t.DartNumber)
            .GroupBy(t => t.RoundNumber);

        foreach (var round in playerThrows)
        {
            var roundTotal = round.Sum(t => t.Points);
            
            // Check for bust in this round
            if (remainingScore - roundTotal < 0 || remainingScore - roundTotal == 1)
            {
                // BUST! Score stays as it was start of round.
                // (Score doesn't change)
                continue; 
            }
            if (remainingScore - roundTotal == 0)
            {
                 // Checked out (potentially - validation happens in RegisterThrow)
                 remainingScore -= roundTotal;
            }
            else
            {
                remainingScore -= roundTotal;
            }
        }

        return remainingScore;
    }

    private async Task UpdateGameStatistics(GameSession game, CancellationToken cancellationToken)
    {
        foreach (var player in game.Players)
        {
            // Skip bot players (empty GUID)
            if (player.UserId == Guid.Empty)
                continue;

            // Compute all round scores for stats
            var roundScores = game.Throws
                .Where(t => t.UserId == player.UserId)
                .GroupBy(t => t.RoundNumber)
                .Select(g => g.Sum(t => t.Points))
                .ToList();

            var checkout = player.IsWinner && game.Throws.Any(t => t.UserId == player.UserId)
                ? game.Throws.Where(t => t.UserId == player.UserId).Last().Points
                : 0;

            await _statisticsService.UpdateStatsAfterGameAsync(
                player.UserId,
                player.IsWinner,
                player.DartsThrown,
                player.PointsScored,
                roundScores,
                checkout,
                cancellationToken);
        }
    }
}
