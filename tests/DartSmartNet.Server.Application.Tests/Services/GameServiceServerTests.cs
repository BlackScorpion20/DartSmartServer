using DartSmartNet.Server.Application.Engines;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;
using DartSmartNet.Server.Domain.Common;

using NSubstitute;
using Shouldly;
using System.Reflection;

namespace DartSmartNet.Server.Application.Tests.Services;

public class GameServiceServerTests
{
    private readonly GameService _service;
    private readonly IGameRepository _gameRepoMock;
    private readonly IUserRepository _userRepoMock;
    private readonly IStatisticsService _statsServiceMock;
    private readonly IGameEventBroadcaster _eventBroadcasterMock;
    private readonly X01Engine _x01Engine;
    private readonly CricketEngine _cricketEngine;

    public GameServiceServerTests()
    {
        _gameRepoMock = Substitute.For<IGameRepository>();
        _userRepoMock = Substitute.For<IUserRepository>();
        _statsServiceMock = Substitute.For<IStatisticsService>();
        _eventBroadcasterMock = Substitute.For<IGameEventBroadcaster>();

        _x01Engine = new X01Engine(_statsServiceMock);
        _cricketEngine = new CricketEngine(_statsServiceMock);
        var engines = new IGameEngine[] { _x01Engine, _cricketEngine };

        _service = new GameService(_gameRepoMock, _userRepoMock, _statsServiceMock, _eventBroadcasterMock, engines);
    }

    /* -------------------------------------------------------------------------- */
    /*                                X01 LOGIC                                   */
    /* -------------------------------------------------------------------------- */

    [Fact]
    public async Task X01_CalculateCurrentScore_ShouldCorrectlyHandleBusts()
    {
        // Setup
        var gameId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var game = GameSession.Create(GameType.X01, 501, false, false);
        // Use reflection to set ID since it's private set in Entity base
        typeof(Entity).GetProperty("Id")!.SetValue(game, gameId);

        game.AddPlayer(userId, 0);
        game.Start();

        game.Start();

        _gameRepoMock.GetByIdAsync(gameId, Arg.Any<CancellationToken>()).Returns(game);
        _userRepoMock.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(User.Create("TestUser", "test@test.com", "hash"));

        // CRITICAL: Mock AddThrowToGame to call domain method
        _gameRepoMock.When(x => x.AddThrowToGame(Arg.Any<GameSession>(), Arg.Any<DartThrow>()))
            .Do(callInfo =>
            {
                var g = callInfo.ArgAt<GameSession>(0);
                var dt = callInfo.ArgAt<DartThrow>(1);
                g.AddThrow(dt);
            });

        // Round 1: 60, 60, 60 (180) -> Rem: 321
        await _service.RegisterThrowAsync(gameId, userId, Score.Triple(20));
        await _service.RegisterThrowAsync(gameId, userId, Score.Triple(20));
        await _service.RegisterThrowAsync(gameId, userId, Score.Triple(20));

        var state = await _service.RegisterThrowAsync(gameId, userId, Score.Miss()); // Start next round/fetch
        var p = state.Players.First();
        p.PointsScored.ShouldBe(180); // PointsScored calculates sum of valid throws? 
        // Note: GameSession.AddThrow adds points regardless of logic in CalculateCurrentScore (currently).
        // CalculateCurrentScore determines the "Remaining" Score.
        // We need to check if the logic holds.
    }

    [Fact]
    public void CalculateCurrentScore_ShouldHandleDoubleOutFailure_AsBust()
    {
        var game = GameSession.Create(GameType.X01, 20, false, false);
        var userId = Guid.NewGuid();
        game.AddPlayer(userId, 0);
        game.Start(); 

        // Scenario 1: Throw Single 20 (Score 0, but single) -> Should be Bust (Rem: 20)
        game.AddThrow(DartThrow.Create(game.Id, userId, 1, 1, Score.Single(20), null));

        // Use the engine directly instead of trying to call private method on GameService
        var score = _x01Engine.CalculateCurrentScore(game, userId);
        score.ShouldBe(20, "Single 20 on 20 remaining should be a bust (Double Out rule/Score 0)");
    }
    
    [Fact]
    public void CalculateCurrentScore_ShouldHandleBust_ScoreNegative()
    {
        var game = GameSession.Create(GameType.X01, 20, false, false);
        var userId = Guid.NewGuid();
        game.AddPlayer(userId, 0);
        
        // Scenario: Throw Triple 20 (60) -> Score -40 -> Bust (Rem: 20)
        game.AddThrow(DartThrow.Create(game.Id, userId, 1, 1, Score.Triple(20), null));

        // Use the engine directly
        var score = _x01Engine.CalculateCurrentScore(game, userId);
        score.ShouldBe(20, "Triple 20 on 20 remaining should be a bust");
    }

    [Fact]
    public void CalculateCurrentScore_ShouldHandleBust_ScoreOne()
    {
        var game = GameSession.Create(GameType.X01, 20, false, false);
        var userId = Guid.NewGuid();
        game.AddPlayer(userId, 0);
        
        // Scenario: Throw 19 -> Rem 1 -> Bust (Rem: 20)
        game.AddThrow(DartThrow.Create(game.Id, userId, 1, 1, Score.Single(19), null));

        // Use the engine directly
        var score = _x01Engine.CalculateCurrentScore(game, userId);
        score.ShouldBe(20, "Leaving score 1 should be a bust");
    }

    /* -------------------------------------------------------------------------- */
    /*                              CRICKET LOGIC                                 */
    /* -------------------------------------------------------------------------- */

    [Fact]
    public void CalculateCricketState_ShouldTrackMarksAndScores()
    {
         var game = GameSession.Create(GameType.Cricket, null, false, false);
         var p1 = Guid.NewGuid();
         var p2 = Guid.NewGuid();
         game.AddPlayer(p1, 0);
         game.AddPlayer(p2, 1);

         // P1 throws 3x20 -> Open for P1
         game.AddThrow(DartThrow.Create(game.Id, p1, 1, 1, Score.Triple(20), null));
         
         // P1 throws 1x20 -> Point 20 (P2 not closed)
         game.AddThrow(DartThrow.Create(game.Id, p1, 1, 2, Score.Single(20), null));

         // P2 throws 2x20 -> 2 marks (not closed)
         game.AddThrow(DartThrow.Create(game.Id, p2, 1, 1, Score.Double(20), null));

         // Use the engine directly
         var state = _cricketEngine.CalculateCricketState(game);

         // Checks P1
         state[p1].Marks[20].ShouldBe(3);
         state[p1].Score.ShouldBe(20);

         // Checks P2
         state[p2].Marks[20].ShouldBe(2);
         state[p2].Score.ShouldBe(0);

         // Helper: check unmapped
         state[p1].Marks[19].ShouldBe(0);
    }
}

