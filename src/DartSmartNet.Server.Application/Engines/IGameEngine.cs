using System;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Domain.ValueObjects;

namespace DartSmartNet.Server.Application.Engines;

public interface IGameEngine
{
    GameType GameType { get; }
    
    int CalculateCurrentScore(GameSession game, Guid userId);
    
    bool CheckWinCondition(GameSession game, Guid userId, out int? finalScore);
    
    Task UpdateStatisticsAsync(GameSession game, CancellationToken cancellationToken = default);

    Dictionary<int, int>? GetPlayerState(GameSession game, Guid userId);
}
