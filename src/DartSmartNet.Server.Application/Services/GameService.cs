using DartSmartNet.Server.Application.DTOs.Game;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.Events;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Services;

public class GameService : IGameService
{
    private readonly IGameRepository _gameRepository;
    private readonly IUserRepository _userRepository;
    private readonly IStatisticsService _statisticsService;
    private readonly IGameEventBroadcaster _eventBroadcaster;
    private static readonly int[] CricketSegments = { 20, 19, 18, 17, 16, 15, 25 };

    public GameService(
        IGameRepository gameRepository,
        IUserRepository userRepository,
        IStatisticsService statisticsService,
        IGameEventBroadcaster eventBroadcaster)
    {
        _gameRepository = gameRepository;
        _userRepository = userRepository;
        _statisticsService = statisticsService;
        _eventBroadcaster = eventBroadcaster;
    }

    public async Task<GameStateDto> CreateGameAsync(
        GameType gameType,
        int? startingScore,
        Guid[] playerIds,
        bool isOnline,
        GameOptions? options = null,
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
        else if (gameType == GameType.Cricket)
        {
            // Cricket doesn't strictly need a starting score, but we can ignore it or use it for CutThroat variants later
        }

        // Check if any player is a bot
        var isBotGame = playerIds.Any(id => id == Guid.Empty);

        // Create game session
        var game = GameSession.Create(gameType, startingScore, isOnline, isBotGame, options);

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

        // Get player username for event
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var playerUsername = user?.Username ?? "Unknown";
        var currentScoreForEvent = game.StartingScore.HasValue 
            ? CalculateCurrentScore(game, userId) 
            : 0;

        // Broadcast throw event
        var throwEvent = new DartsThrowEvent(
            gameId,
            DateTime.UtcNow,
            game.GameType.ToString(),
            playerUsername,
            currentScoreForEvent,
            dartNumber,
            score.Segment,
            (int)score.Multiplier,
            score.Points
        );
        await _eventBroadcaster.BroadcastEventAsync(throwEvent, cancellationToken);

        // Check for game completion
        if (IsX01Game(game.GameType) && game.StartingScore.HasValue)
        {
            var currentScore = CalculateCurrentScore(game, userId);

            if (currentScore == 0)
            {
                // Player finished (CalculateCurrentScore validates In/Out logic)
                game.Complete(userId);
                player.SetFinalScore(0);
                
                // Broadcast game won event
                var wonEvent = new GameWonEvent(
                    gameId,
                    DateTime.UtcNow,
                    game.GameType.ToString(),
                    playerUsername,
                    player.DartsThrown,
                    player.PointsScored,
                    (double)player.PPD
                );
                await _eventBroadcaster.BroadcastEventAsync(wonEvent, cancellationToken);
                
                // Update statistics
                await UpdateGameStatistics(game, cancellationToken);
            }
            // If currentScore > 0 or < 0 (if logic allowed), game continues.
            // CalculateCurrentScore returns 'previous score' if bust, so it won't be 0.
        }
        else if (game.GameType == GameType.Cricket)
        {
            var cricketState = CalculateCricketState(game);
            var playerState = cricketState[userId];

            // Update PointsScored in the game session based on the calculation
            // Note: Currently GameSession stores Points per throw, but Cricket points are derived.
            // We might need to store the "effective points" in the throw or just rely on state.
            // For now, we rely on CalculateCricketState for the "Game State" truth.

            // Check Win Condition:
            // 1. User has all numbers closed
            var allClosed = CricketSegments.All(s => playerState.Marks.ContainsKey(s) && playerState.Marks[s] >= 3);
            
            // 2. User has highest (or equal) score
            var playerScore = playerState.Score;
            var isHighestScore = cricketState.All(kv => kv.Value.Score <= playerScore);

            if (allClosed && isHighestScore)
            {
                game.Complete(userId);
                // For Cricket, 'FinalScore' typically refers to the Points total
                player.SetFinalScore(playerScore);
                
                // Broadcast game won event for Cricket
                var wonEvent = new GameWonEvent(
                    gameId,
                    DateTime.UtcNow,
                    game.GameType.ToString(),
                    playerUsername,
                    player.DartsThrown,
                    playerScore,
                    (double)player.PPD
                );
                await _eventBroadcaster.BroadcastEventAsync(wonEvent, cancellationToken);
                
                await UpdateGameStatistics(game, cancellationToken);
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
            // Determine winner based on current scores
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
            else if (game.GameType == GameType.Cricket)
            {
                var cricketState = CalculateCricketState(game);
                
                // Winner is one with all closed and highest score, OR if forced end, typically highest score?
                // If forced end, we'll just take highest score for now.
                var winner = cricketState
                    .OrderByDescending(kv => kv.Value.Score)
                     // Tie-breaker: Most marks?
                    .ThenByDescending(kv => kv.Value.Marks.Values.Sum())
                    .FirstOrDefault();

                // If default struct (empty), winner.Key might be empty
                if (winner.Key != Guid.Empty)
                {
                    game.Complete(winner.Key);
                    var winningPlayer = game.Players.First(p => p.UserId == winner.Key);
                    winningPlayer.SetFinalScore(winner.Value.Score);
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

        // Update statistics if game was completed or ended manually
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

        // If Cricket, populate Marks and Points
        if (game.GameType == GameType.Cricket)
        {
            var cricketState = CalculateCricketState(game);
            // Re-map players with Cricket Data
            players.Clear();
            foreach (var player in game.Players.OrderBy(p => p.PlayerOrder))
            {
                var user = await _userRepository.GetByIdAsync(player.UserId, cancellationToken);
                var state = cricketState[player.UserId];
                
                players.Add(new PlayerGameDto(
                    player.UserId,
                    user?.Username ?? "Unknown",
                    player.PlayerOrder,
                    player.FinalScore, // Or state.Score? FinalScore is set on win.
                    player.DartsThrown,
                    state.Score, // Use Cricket Score (Points)
                    player.PPD, // PPD might need adjustment for MPR (Marks Per Round)
                    player.IsWinner,
                    state.Marks
                ));
            }
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
            currentPlayerIndex,
            game.Options
        );
    }

    private bool IsX01Game(GameType gameType)
    {
        return gameType == GameType.X01;
    }



    private bool HasOpened(GameSession game, Guid userId, int roundNumber)
    {
        // For Double In: Check if any previous throw (or current round before this throw?) was a double.
        // Actually, we need to check ALL throws up to the current point being calculated.
        
        // Optimize: Store 'HasOpened' in player state? For now, iterate.
        var playerThrows = game.Throws
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.ThrownAt)
            .ToList();

        foreach (var t in playerThrows)
        {
            if (t.Multiplier == Multiplier.Double) return true;
        }
        return false;
    }

    private int CalculateCurrentScore(GameSession game, Guid userId)
    {
        if (!game.StartingScore.HasValue)
            return 0;

        var remainingScore = game.StartingScore.Value;
        var hasOpened = game.Options.InMode != "Double"; // If not Double In, we are open by default.

        var playerThrows = game.Throws
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.RoundNumber)
            .ThenBy(t => t.DartNumber)
            .GroupBy(t => t.RoundNumber);

        foreach (var round in playerThrows)
        {
            var roundTotal = 0;
            var roundBust = false;

            // Process darts individually to handle "Double In" within a round
            foreach(var dart in round)
            {
                if (!hasOpened)
                {
                    if (dart.Multiplier == Multiplier.Double)
                    {
                        hasOpened = true;
                        // First double counts!
                        // Logic check: "Double In" usually means points start counting FROM the double.
                        // Does the double value itself subtract? Yes.
                    }
                    else
                    {
                        // Throw doesn't count if not open yet
                        continue; 
                    }
                }

                // If we are here, we are open (either was open, or just opened)
                var newScore = remainingScore - roundTotal - dart.Points;

                // 1. Check Busts
                // a) Below Zero
                if (newScore < 0)
                {
                    roundBust = true;
                    break;
                }
                // b) Score of 1 (Invalid for any Out that requires Double/Triple?)
                // Actually, 1 is always invalid because you cannot checkout 1 with a Double/Triple.
                // Even for Single Out? Single Out 1 -> Hit 1 -> 0. OK.
                // But Double Out 1 -> invalid (need D0.5 impossible).
                // Master Out 1 -> invalid.
                if (newScore == 1 && game.Options.OutMode != "Straight")
                {
                    roundBust = true;
                    break;
                }
                // c) Reached Zero
                if (newScore == 0)
                {
                    // Check Out Condition
                    var validOut = false;
                    if (game.Options.OutMode == "Double" && dart.Multiplier == Multiplier.Double) validOut = true;
                    else if (game.Options.OutMode == "Master" && (dart.Multiplier == Multiplier.Double || dart.Multiplier == Multiplier.Triple)) validOut = true;
                    else if (game.Options.OutMode == "Straight") validOut = true;

                    if (!validOut)
                    {
                        roundBust = true;
                        break;
                    }
                    
                    // Valid win!
                    roundTotal += dart.Points;
                    // Ignore remaining darts in this round (game over)
                    goto RoundFinished; 
                }

                roundTotal += dart.Points;
            }

            if (!roundBust)
            {
                remainingScore -= roundTotal;
            }
            // If bust, remainingScore stays same (ignore roundTotal)

            RoundFinished:;
            if (remainingScore == 0) break; // Game Over
        }

        return remainingScore;
    }

    private async Task UpdateGameStatistics(GameSession game, CancellationToken cancellationToken)
    {
        if (!game.StartingScore.HasValue) return;

        foreach (var player in game.Players)
        {
            // Skip bot players (empty GUID)
            if (player.UserId == Guid.Empty)
                continue;

            // Recalculate VALID points and darts for this player
            var validPoints = 0;
            var totalDarts = 0;
            var currentTotal = game.StartingScore.Value;
            var hasOpened = game.Options.InMode != "Double";
            
            var roundScoresList = new List<int>();

            var roundGroups = game.Throws
                .Where(t => t.UserId == player.UserId)
                .OrderBy(t => t.RoundNumber)
                .ThenBy(t => t.DartNumber)
                .GroupBy(t => t.RoundNumber);

            foreach (var round in roundGroups)
            {
                var roundPoints = 0;
                var roundDarts = 0;
                var roundBust = false;

                foreach (var dart in round)
                {
                    roundDarts++;
                    if (!hasOpened)
                    {
                        if (dart.Multiplier == Multiplier.Double)
                            hasOpened = true;
                        else
                            continue; // Doesn't count towards score
                    }

                    var tempScore = currentTotal - roundPoints - dart.Points;
                    
                    // Bust logic
                    if (tempScore < 0 || (tempScore == 1 && game.Options.OutMode != "Straight"))
                    {
                        roundBust = true;
                        break;
                    }
                    
                    if (tempScore == 0)
                    {
                        var validOut = (game.Options.OutMode == "Straight") || 
                                     (game.Options.OutMode == "Double" && dart.Multiplier == Multiplier.Double) ||
                                     (game.Options.OutMode == "Master" && (dart.Multiplier == Multiplier.Double || dart.Multiplier == Multiplier.Triple));

                        if (!validOut)
                        {
                            roundBust = true;
                            break;
                        }
                        
                        roundPoints += dart.Points;
                        currentTotal = 0;
                        goto ProcessingComplete;
                    }

                    roundPoints += dart.Points;
                }

                if (!roundBust)
                {
                    validPoints += roundPoints;
                    currentTotal -= roundPoints;
                    roundScoresList.Add(roundPoints);
                }
                else
                {
                    roundScoresList.Add(0); // Bust round counts as 0 points
                }
                totalDarts += roundDarts;
            }

            ProcessingComplete:
            if (currentTotal == 0)
            {
                // This handles cases where we hit 0 in the inner loop
                validPoints = game.StartingScore.Value;
                // totalDarts is already updated
            }

            // Update the GamePlayer entity with verified stats
            player.OverrideStats(validPoints, totalDarts);

            var checkout = player.IsWinner && currentTotal == 0 && game.Throws.Any(t => t.UserId == player.UserId)
                ? game.Throws.Where(t => t.UserId == player.UserId).Last().Points
                : 0;

            await _statisticsService.UpdateStatsAfterGameAsync(
                player.UserId,
                player.IsWinner,
                totalDarts,
                validPoints,
                roundScoresList,
                checkout,
                cancellationToken);
        }
    }

    private Dictionary<Guid, (int Score, Dictionary<int, int> Marks)> CalculateCricketState(GameSession game)
    {
        // Initialize state for all players
        var state = game.Players.ToDictionary(
            p => p.UserId,
            p => (Score: 0, Marks: CricketSegments.ToDictionary(s => s, s => 0))
        );

        // Process throws in order
        var sortedThrows = game.Throws.OrderBy(t => t.ThrownAt).ToList();

        foreach (var t in sortedThrows)
        {
            // Skip invalid segments (misses or non-cricket numbers)
            if (!CricketSegments.Contains(t.Segment)) continue;

            var playerId = t.UserId;
            // Handle Bullseye: Outer=25 (1 mark), Inner=50 (2 marks)
            // StartSmartNet.Server.Domain.ValueObjects.Score has Multiplier.
            // For Bull: SingleBull is Segment 25, Multiplier 1 (1 mark)
            // DoubleBull is Segment 25, Multiplier 2 (2 marks)
            var marksToAdd = (int)t.Multiplier; 
            var segment = t.Segment;

            var playerState = state[playerId];
            var currentMarks = playerState.Marks[segment];

            // Calculate new status
            // Case 1: Filling marks to close (up to 3)
            // Case 2: Scoring points (excess marks)

            for (int m = 0; m < marksToAdd; m++)
            {
                if (currentMarks < 3)
                {
                    currentMarks++;
                    playerState.Marks[segment] = currentMarks;
                }
                else
                {
                    // Already closed by me. Check opponents.
                    // Score points ONLY if NOT closed by ALL opponents.
                    var opponents = state.Keys.Where(k => k != playerId);
                    var isClosedByAllOpponents = opponents.All(oId => state[oId].Marks[segment] >= 3);

                    if (!isClosedByAllOpponents)
                    {
                        playerState.Score += segment;
                    }
                }
            }
            
            // Update the tuple in the dictionary
            state[playerId] = (playerState.Score, playerState.Marks);
        }

        return state;
    }
}
