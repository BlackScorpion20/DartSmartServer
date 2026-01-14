using DartSmartNet.Server.Domain.Common;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Domain.Entities;

public class GameSession : Entity
{
    public GameType GameType { get; private set; }
    public int? StartingScore { get; private set; }
    public GameStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public Guid? WinnerId { get; private set; }
    public bool IsOnline { get; private set; }
    public bool IsBotGame { get; private set; }

    // Navigation properties
    public User? Winner { get; private set; }
    public List<GamePlayer> Players { get; private set; }
    public List<DartThrow> Throws { get; private set; }

    private GameSession() : base()
    {
        Players = new List<GamePlayer>();
        Throws = new List<DartThrow>();
        Status = GameStatus.WaitingForPlayers;
        StartedAt = DateTime.UtcNow;
    }

    public static GameSession Create(GameType gameType, int? startingScore, bool isOnline, bool isBotGame)
    {
        return new GameSession
        {
            GameType = gameType,
            StartingScore = startingScore,
            IsOnline = isOnline,
            IsBotGame = isBotGame,
            Status = GameStatus.WaitingForPlayers,
            StartedAt = DateTime.UtcNow
        };
    }

    public void AddPlayer(Guid userId, int playerOrder)
    {
        var player = GamePlayer.Create(Id, userId, playerOrder);
        Players.Add(player);
    }

    public void Start()
    {
        Status = GameStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(Guid winnerId)
    {
        Status = GameStatus.Completed;
        WinnerId = winnerId;
        EndedAt = DateTime.UtcNow;

        // Mark winner
        var winner = Players.FirstOrDefault(p => p.UserId == winnerId);
        if (winner != null)
            winner.MarkAsWinner();
    }

    public void Abandon()
    {
        Status = GameStatus.Abandoned;
        EndedAt = DateTime.UtcNow;
    }

    public void AddThrow(DartThrow dartThrow)
    {
        Throws.Add(dartThrow);

        // Update player stats
        var player = Players.FirstOrDefault(p => p.UserId == dartThrow.UserId);
        if (player != null)
        {
            player.IncrementDartsThrown();
            player.AddPoints(dartThrow.Points);
        }
    }
}
