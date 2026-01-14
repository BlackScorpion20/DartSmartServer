using DartSmartNet.Server.Domain.Common;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Domain.Entities;

public class TrainingSession : Entity
{
    public Guid UserId { get; private set; }
    public GameType TrainingType { get; private set; }
    public TrainingStatus Status { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }
    public int CurrentTarget { get; private set; }
    public int Score { get; private set; }
    public int DartsThrown { get; private set; }
    public int SuccessfulHits { get; private set; }

    // Navigation properties
    public User? User { get; private set; }
    public List<TrainingThrow> Throws { get; private set; }

    private TrainingSession() : base()
    {
        Throws = new List<TrainingThrow>();
        Status = TrainingStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        CurrentTarget = 1; // Default starting target
    }

    public static TrainingSession Create(Guid userId, GameType trainingType)
    {
        if (!IsTrainingMode(trainingType))
        {
            throw new InvalidOperationException($"{trainingType} is not a training mode");
        }

        var session = new TrainingSession
        {
            UserId = userId,
            TrainingType = trainingType,
            Status = TrainingStatus.InProgress,
            StartedAt = DateTime.UtcNow,
            CurrentTarget = GetInitialTarget(trainingType),
            Score = 0,
            DartsThrown = 0,
            SuccessfulHits = 0
        };

        return session;
    }

    public void RegisterThrow(Score score, bool wasSuccessful)
    {
        if (Status != TrainingStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot register throw in {Status} session");
        }

        var trainingThrow = TrainingThrow.Create(Id, score, wasSuccessful);
        Throws.Add(trainingThrow);

        DartsThrown++;
        Score += score.Points;

        if (wasSuccessful)
        {
            SuccessfulHits++;
            AdvanceTarget();
        }
    }

    public void Complete()
    {
        Status = TrainingStatus.Completed;
        EndedAt = DateTime.UtcNow;
    }

    public void Abandon()
    {
        Status = TrainingStatus.Abandoned;
        EndedAt = DateTime.UtcNow;
    }

    public decimal AccuracyPercentage => DartsThrown > 0
        ? Math.Round((decimal)SuccessfulHits / DartsThrown * 100, 2)
        : 0;

    private void AdvanceTarget()
    {
        CurrentTarget = TrainingType switch
        {
            GameType.TrainingRoundTheBoard => CurrentTarget < 20 ? CurrentTarget + 1 : 20,
            GameType.TrainingBobsDoubles => CurrentTarget < 20 ? CurrentTarget + 1 : 20,
            GameType.TrainingBobsTriples => CurrentTarget < 20 ? CurrentTarget + 1 : 20,
            GameType.TrainingShanghaiDrill => CurrentTarget < 20 ? CurrentTarget + 1 : 20,
            _ => CurrentTarget
        };

        // Check if training is complete
        if (IsTrainingComplete())
        {
            Complete();
        }
    }

    private bool IsTrainingComplete()
    {
        return TrainingType switch
        {
            GameType.TrainingRoundTheBoard => CurrentTarget == 20 && SuccessfulHits >= 20,
            GameType.TrainingBobsDoubles => CurrentTarget == 20 && SuccessfulHits >= 20,
            GameType.TrainingBobsTriples => CurrentTarget == 20 && SuccessfulHits >= 20,
            GameType.TrainingShanghaiDrill => CurrentTarget == 20 && SuccessfulHits >= 60, // 3 hits per number (S-D-T)
            GameType.TrainingBullseye => SuccessfulHits >= 25, // Hit 25 bullseyes
            GameType.TrainingSegmentFocus => DartsThrown >= 30, // 30 darts practice
            GameType.TrainingCheckoutPractice => SuccessfulHits >= 10, // Complete 10 checkouts
            _ => false
        };
    }

    private static bool IsTrainingMode(GameType gameType)
    {
        return gameType is GameType.TrainingBobsDoubles
            or GameType.TrainingBobsTriples
            or GameType.TrainingBullseye
            or GameType.TrainingSegmentFocus
            or GameType.TrainingCheckoutPractice
            or GameType.TrainingRoundTheBoard
            or GameType.TrainingShanghaiDrill;
    }

    private static int GetInitialTarget(GameType trainingType)
    {
        return trainingType switch
        {
            GameType.TrainingRoundTheBoard => 1,
            GameType.TrainingBobsDoubles => 1,
            GameType.TrainingBobsTriples => 1,
            GameType.TrainingShanghaiDrill => 1,
            GameType.TrainingBullseye => 25,
            GameType.TrainingSegmentFocus => 20, // Default to T20
            GameType.TrainingCheckoutPractice => 40, // Start with common checkout
            _ => 0
        };
    }
}

public enum TrainingStatus
{
    InProgress,
    Completed,
    Abandoned
}
