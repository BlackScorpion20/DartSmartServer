using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.Interfaces;

public interface IGameRepository
{
    Task<GameSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameSession>> GetActiveGamesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<GameSession>> GetUserGamesAsync(Guid userId, int limit = 20, CancellationToken cancellationToken = default);
    Task AddAsync(GameSession game, CancellationToken cancellationToken = default);
    Task UpdateAsync(GameSession game, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameSession>> GetGamesByTypeAsync(GameType gameType, CancellationToken cancellationToken = default);
}
