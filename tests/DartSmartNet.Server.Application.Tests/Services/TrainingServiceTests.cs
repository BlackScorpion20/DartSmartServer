using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DartSmartNet.Server.Application.Tests.Services;

public class TrainingServiceTests
{
    private readonly ITrainingRepository _trainingRepository;
    private readonly TrainingService _sut;

    public TrainingServiceTests()
    {
        _trainingRepository = Substitute.For<ITrainingRepository>();
        _sut = new TrainingService(_trainingRepository);
    }

    [Fact]
    public async Task StartTrainingAsync_WithValidTrainingType_ShouldCreateSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var trainingType = GameType.TrainingBobsDoubles;

        // Act
        var result = await _sut.StartTrainingAsync(userId, trainingType);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(userId);
        result.TrainingType.ShouldBe(trainingType);
        result.Status.ShouldBe(TrainingStatus.InProgress);
        result.DartsThrown.ShouldBe(0);
        result.SuccessfulHits.ShouldBe(0);

        await _trainingRepository.Received(1).AddAsync(Arg.Any<TrainingSession>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(GameType.X01)]
    [InlineData(GameType.Cricket)]
    public async Task StartTrainingAsync_WithNonTrainingType_ShouldThrowException(GameType gameType)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.StartTrainingAsync(userId, gameType));
    }

    [Fact]
    public async Task RegisterTrainingThrowAsync_RoundTheBoard_WithCorrectTarget_ShouldMarkAsSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingRoundTheBoard);

        _trainingRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var score = Score.Single(1); // First target in Round the Board

        // Act
        var result = await _sut.RegisterTrainingThrowAsync(sessionId, score);

        // Assert
        result.SuccessfulHits.ShouldBe(1);
        result.CurrentTarget.ShouldBe(2); // Advanced to next target
        result.DartsThrown.ShouldBe(1);

        await _trainingRepository.Received(1).UpdateAsync(Arg.Any<TrainingSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterTrainingThrowAsync_BobsDoubles_WithCorrectDouble_ShouldMarkAsSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);

        _trainingRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var score = Score.Double(1); // Double 1

        // Act
        var result = await _sut.RegisterTrainingThrowAsync(sessionId, score);

        // Assert
        result.SuccessfulHits.ShouldBe(1);
        result.CurrentTarget.ShouldBe(2);
    }

    [Fact]
    public async Task RegisterTrainingThrowAsync_BobsDoubles_WithWrongMultiplier_ShouldNotMarkAsSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);

        _trainingRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var score = Score.Single(1); // Single instead of double

        // Act
        var result = await _sut.RegisterTrainingThrowAsync(sessionId, score);

        // Assert
        result.SuccessfulHits.ShouldBe(0);
        result.CurrentTarget.ShouldBe(1); // Target doesn't advance
    }

    [Fact]
    public async Task RegisterTrainingThrowAsync_Bullseye_WithBullHit_ShouldMarkAsSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBullseye);

        _trainingRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var score = Score.DoubleBull();

        // Act
        var result = await _sut.RegisterTrainingThrowAsync(sessionId, score);

        // Assert
        result.SuccessfulHits.ShouldBe(1);
        result.Score.ShouldBe(50);
    }

    [Fact]
    public async Task RegisterTrainingThrowAsync_WithCompletedSession_ShouldThrowException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);
        session.Complete();

        _trainingRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        var score = Score.Double(1);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.RegisterTrainingThrowAsync(sessionId, score));
    }

    [Fact]
    public async Task EndTrainingAsync_WithInProgressSession_ShouldAbandonSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);

        _trainingRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        // Act
        var result = await _sut.EndTrainingAsync(sessionId);

        // Assert
        result.Status.ShouldBe(TrainingStatus.Abandoned);
        result.EndedAt.ShouldNotBeNull();

        await _trainingRepository.Received(1).UpdateAsync(Arg.Any<TrainingSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTrainingSessionAsync_WithExistingSession_ShouldReturnSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = TrainingSession.Create(Guid.NewGuid(), GameType.TrainingBobsDoubles);

        _trainingRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(session);

        // Act
        var result = await _sut.GetTrainingSessionAsync(sessionId);

        // Assert
        result.ShouldNotBeNull();
        result.TrainingType.ShouldBe(GameType.TrainingBobsDoubles);
        result.Status.ShouldBe(TrainingStatus.InProgress);
    }

    [Fact]
    public async Task GetTrainingSessionAsync_WithNonExistingSession_ShouldThrowException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _trainingRepository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((TrainingSession?)null);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _sut.GetTrainingSessionAsync(sessionId));
    }

    [Fact]
    public async Task GetUserTrainingHistoryAsync_ShouldReturnHistory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessions = new List<TrainingSession>
        {
            TrainingSession.Create(userId, GameType.TrainingBobsDoubles),
            TrainingSession.Create(userId, GameType.TrainingBullseye)
        };

        _trainingRepository.GetUserTrainingHistoryAsync(userId, 20, Arg.Any<CancellationToken>())
            .Returns(sessions);

        // Act
        var result = await _sut.GetUserTrainingHistoryAsync(userId);

        // Assert
        result.ShouldNotBeEmpty();
        result.Count().ShouldBe(2);
    }
}
