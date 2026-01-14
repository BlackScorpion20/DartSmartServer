using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace DartSmartNet.Server.Domain.Tests.Entities;

public class TrainingSessionTests
{
    [Fact]
    public void Create_WithValidTrainingType_ShouldCreateSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trainingType = GameType.TrainingBobsDoubles;

        // Act
        var session = TrainingSession.Create(userId, trainingType);

        // Assert
        session.ShouldNotBeNull();
        session.UserId.ShouldBe(userId);
        session.TrainingType.ShouldBe(trainingType);
        session.Status.ShouldBe(TrainingStatus.InProgress);
        session.CurrentTarget.ShouldBe(1);
        session.DartsThrown.ShouldBe(0);
        session.SuccessfulHits.ShouldBe(0);
        session.AccuracyPercentage.ShouldBe(0);
    }

    [Fact]
    public void Create_WithNonTrainingType_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var gameType = GameType.X01;

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            TrainingSession.Create(userId, gameType));
    }

    [Fact]
    public void RegisterThrow_WithSuccessfulHit_ShouldIncrementCounters()
    {
        // Arrange
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);
        var score = Score.Double(1);

        // Act
        session.RegisterThrow(score, wasSuccessful: true);

        // Assert
        session.DartsThrown.ShouldBe(1);
        session.SuccessfulHits.ShouldBe(1);
        session.Score.ShouldBe(2);
        session.AccuracyPercentage.ShouldBe(100m);
        session.CurrentTarget.ShouldBe(2);
    }

    [Fact]
    public void RegisterThrow_WithMiss_ShouldOnlyIncrementDartsThrown()
    {
        // Arrange
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);
        var score = Score.Miss();

        // Act
        session.RegisterThrow(score, wasSuccessful: false);

        // Assert
        session.DartsThrown.ShouldBe(1);
        session.SuccessfulHits.ShouldBe(0);
        session.Score.ShouldBe(0);
        session.AccuracyPercentage.ShouldBe(0m);
        session.CurrentTarget.ShouldBe(1); // Target doesn't advance
    }

    [Fact]
    public void RegisterThrow_OnCompletedSession_ShouldThrowException()
    {
        // Arrange
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);
        session.Complete();
        var score = Score.Double(1);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            session.RegisterThrow(score, true));
    }

    [Fact]
    public void Complete_ShouldSetStatusAndEndTime()
    {
        // Arrange
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);

        // Act
        session.Complete();

        // Assert
        session.Status.ShouldBe(TrainingStatus.Completed);
        session.EndedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Abandon_ShouldSetStatusAndEndTime()
    {
        // Arrange
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);

        // Act
        session.Abandon();

        // Assert
        session.Status.ShouldBe(TrainingStatus.Abandoned);
        session.EndedAt.ShouldNotBeNull();
    }

    [Fact]
    public void AccuracyPercentage_WithHitsAndMisses_ShouldCalculateCorrectly()
    {
        // Arrange
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);

        // Act - 3 hits, 7 misses = 30%
        for (int i = 0; i < 3; i++)
            session.RegisterThrow(Score.Double(1), true);
        for (int i = 0; i < 7; i++)
            session.RegisterThrow(Score.Miss(), false);

        // Assert
        session.DartsThrown.ShouldBe(10);
        session.SuccessfulHits.ShouldBe(3);
        session.AccuracyPercentage.ShouldBe(30m);
    }

    [Theory]
    [InlineData(GameType.TrainingRoundTheBoard, 1)]
    [InlineData(GameType.TrainingBobsDoubles, 1)]
    [InlineData(GameType.TrainingBobsTriples, 1)]
    [InlineData(GameType.TrainingShanghaiDrill, 1)]
    [InlineData(GameType.TrainingBullseye, 25)]
    [InlineData(GameType.TrainingCheckoutPractice, 40)]
    public void Create_ShouldSetCorrectInitialTarget(GameType trainingType, int expectedTarget)
    {
        // Arrange & Act
        var session = TrainingSession.Create(Guid.NewGuid(), trainingType);

        // Assert
        session.CurrentTarget.ShouldBe(expectedTarget);
    }
}
