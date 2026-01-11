using DartSmart.Domain.Common;
using DartSmart.Domain.ValueObjects;

namespace DartSmart.Domain.Entities;

/// <summary>
/// Game aggregate root - manages game state and rules
/// </summary>
public class Game : Entity<GameId>, IAggregateRoot
{
    private readonly List<GamePlayer> _players = new();
    private readonly List<DartThrow> _throws = new();

    public GameType GameType { get; private init; }
    public int StartScore { get; private init; }
    public X01InMode InMode { get; private init; }
    public X01OutMode OutMode { get; private init; }
    public GameStatus Status { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? FinishedAt { get; private set; }
    public PlayerId? WinnerId { get; private set; }
    public int CurrentPlayerIndex { get; private set; }
    public int CurrentRound { get; private set; }

    public IReadOnlyCollection<GamePlayer> Players => _players.AsReadOnly();
    public IReadOnlyCollection<DartThrow> Throws => _throws.AsReadOnly();

    private Game() { } // EF Core constructor

    public static Game Create(
        GameType gameType,
        int startScore,
        X01InMode inMode = X01InMode.StraightIn,
        X01OutMode outMode = X01OutMode.DoubleOut)
    {
        if (IsX01Game(gameType) && startScore <= 0)
            throw new ArgumentException("Start score must be positive for X01 games", nameof(startScore));

        return new Game
        {
            Id = GameId.New(),
            GameType = gameType,
            StartScore = startScore,
            InMode = inMode,
            OutMode = outMode,
            Status = GameStatus.Lobby,
            CreatedAt = DateTime.UtcNow,
            CurrentPlayerIndex = 0,
            CurrentRound = 1
        };
    }

    public void AddPlayer(PlayerId playerId)
    {
        if (Status != GameStatus.Lobby)
            throw new InvalidOperationException("Cannot add players after game has started");

        if (_players.Any(p => p.PlayerId == playerId))
            throw new InvalidOperationException("Player already in game");

        var turnOrder = _players.Count;
        var gamePlayer = GamePlayer.Create(Id, playerId, StartScore, turnOrder);
        _players.Add(gamePlayer);
    }

    public void RemovePlayer(PlayerId playerId)
    {
        if (Status != GameStatus.Lobby)
            throw new InvalidOperationException("Cannot remove players after game has started");

        var player = _players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player is null)
            throw new InvalidOperationException("Player not in game");

        _players.Remove(player);
    }

    public void Start()
    {
        if (Status != GameStatus.Lobby)
            throw new InvalidOperationException("Game already started");

        if (_players.Count < 1)
            throw new InvalidOperationException("Need at least 1 player to start");

        Status = GameStatus.InProgress;
        CurrentRound = 1;
        CurrentPlayerIndex = 0;
    }

    public GamePlayer? GetCurrentPlayer()
    {
        if (Status != GameStatus.InProgress || _players.Count == 0)
            return null;

        return _players[CurrentPlayerIndex];
    }

    public int GetPlayerScore(PlayerId playerId)
    {
        var player = _players.FirstOrDefault(p => p.PlayerId == playerId);
        return player?.CurrentScore ?? 0;
    }

    public DartThrow RegisterThrow(PlayerId playerId, int segment, int multiplier, int dartNumber)
    {
        if (Status != GameStatus.InProgress)
            throw new InvalidOperationException("Game is not in progress");

        var currentPlayer = GetCurrentPlayer();
        if (currentPlayer is null || currentPlayer.PlayerId != playerId)
            throw new InvalidOperationException("Not this player's turn");

        var scoreBeforeThrow = currentPlayer.CurrentScore;
        var points = segment * multiplier;

        // Check for bust in X01
        bool isBust = false;
        if (IsX01Game(GameType))
        {
            var newScore = scoreBeforeThrow - points;

            // Bust conditions for X01
            if (newScore < 0)
                isBust = true;
            else if (newScore == 0 && OutMode == X01OutMode.DoubleOut && multiplier != 2)
                isBust = true;
            else if (newScore == 0 && OutMode == X01OutMode.MasterOut && multiplier < 2)
                isBust = true;
            else if (newScore == 1 && OutMode != X01OutMode.StraightOut)
                isBust = true;
        }

        var dartThrow = DartThrow.Create(Id, playerId, segment, multiplier, CurrentRound, dartNumber, isBust);
        _throws.Add(dartThrow);

        if (!isBust && IsX01Game(GameType))
        {
            currentPlayer.SubtractScore(points);

            // Check for win
            if (currentPlayer.CurrentScore == 0)
            {
                FinishGame(playerId);
            }
        }

        currentPlayer.IncrementDartsThrown();

        return dartThrow;
    }

    public void NextPlayer()
    {
        if (Status != GameStatus.InProgress)
            throw new InvalidOperationException("Game is not in progress");

        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _players.Count;
        if (CurrentPlayerIndex == 0)
        {
            CurrentRound++;
        }
    }

    public void FinishGame(PlayerId winnerId)
    {
        if (Status == GameStatus.Finished)
            throw new InvalidOperationException("Game already finished");

        Status = GameStatus.Finished;
        WinnerId = winnerId;
        FinishedAt = DateTime.UtcNow;
    }

    private static bool IsX01Game(GameType gameType) =>
        gameType is GameType.X01_301 or GameType.X01_501 or GameType.X01_701;
}
