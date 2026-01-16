using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Domain.Entities;

namespace DartSmartNet.Server.Application.Interfaces;

public interface IGameProfileRepository
{
    Task<GameProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameProfile>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameProfile>> GetPublicProfilesAsync(int limit = 50, CancellationToken cancellationToken = default);
    Task AddAsync(GameProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(GameProfile profile, CancellationToken cancellationToken = default);
    Task DeleteAsync(GameProfile profile, CancellationToken cancellationToken = default);
}
