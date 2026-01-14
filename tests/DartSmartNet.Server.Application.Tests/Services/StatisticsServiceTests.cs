using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace DartSmartNet.Server.Application.Tests.Services;

public class StatisticsServiceTests
{
    private readonly IStatsRepository _statsRepository;
    private readonly IUserRepository _userRepository;
    private readonly StatisticsService _sut;

    public StatisticsServiceTests()
    {
        _statsRepository = Substitute.For<IStatsRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new StatisticsService(_statsRepository, _userRepository);
    }

    [Fact]
    public async Task GetUserStatsAsync_WithExistingStats_ShouldReturnStats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var stats = PlayerStats.CreateForUser(userId);
        stats.UpdateAfterGame(true, 30, 450, new[] { 60, 60, 60, 140, 80, 50 }, 50);

        var user = User.Create("testuser", "test@test.com", "hash");

        _statsRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(stats);
        _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.GetUserStatsAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(userId);
        result.Username.ShouldBe("testuser");
        result.GamesPlayed.ShouldBe(1);
        result.GamesWon.ShouldBe(1);
        result.AveragePPD.ShouldBe(15m);
    }

    [Fact]
    public async Task GetUserStatsAsync_WithNonExistingStats_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _statsRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((PlayerStats?)null);

        // Act
        var result = await _sut.GetUserStatsAsync(userId);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetLeaderboardAsync_ShouldReturnSortedLeaderboard()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var stats1 = PlayerStats.CreateForUser(userId1);
        var stats2 = PlayerStats.CreateForUser(userId2);

        var statsList = new List<PlayerStats> { stats1, stats2 };

        _statsRepository.GetLeaderboardAsync(100, Arg.Any<CancellationToken>())
            .Returns(statsList);

        var user1 = User.Create("player1", "player1@test.com", "hash");
        var user2 = User.Create("player2", "player2@test.com", "hash");

        _userRepository.GetByIdAsync(userId1, Arg.Any<CancellationToken>())
            .Returns(user1);
        _userRepository.GetByIdAsync(userId2, Arg.Any<CancellationToken>())
            .Returns(user2);

        // Act
        var result = await _sut.GetLeaderboardAsync(100);

        // Assert
        result.ShouldNotBeEmpty();
        result.Count().ShouldBe(2);
    }

    [Fact]
    public async Task UpdateStatsAfterGameAsync_WithNewUser_ShouldCreateStats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _statsRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((PlayerStats?)null);

        var roundScores = new[] { 60, 60, 60, 140, 80, 50 };

        // Act
        await _sut.UpdateStatsAfterGameAsync(userId, true, 30, 450, roundScores, 50);

        // Assert
        await _statsRepository.Received(1).AddAsync(Arg.Any<PlayerStats>(), Arg.Any<CancellationToken>());
        await _statsRepository.DidNotReceive().UpdateAsync(Arg.Any<PlayerStats>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStatsAfterGameAsync_WithExistingUser_ShouldUpdateStats()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingStats = PlayerStats.CreateForUser(userId);

        _statsRepository.GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(existingStats);

        var roundScores = new[] { 60, 60, 60, 180, 81 };

        // Act
        await _sut.UpdateStatsAfterGameAsync(userId, true, 27, 441, roundScores, 81);

        // Assert
        await _statsRepository.Received(1).UpdateAsync(Arg.Any<PlayerStats>(), Arg.Any<CancellationToken>());
        await _statsRepository.DidNotReceive().AddAsync(Arg.Any<PlayerStats>(), Arg.Any<CancellationToken>());
    }
}
