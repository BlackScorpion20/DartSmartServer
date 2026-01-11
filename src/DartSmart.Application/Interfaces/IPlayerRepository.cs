using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;

namespace DartSmart.Application.Interfaces;

/// <summary>
/// Repository interface for Player aggregate
/// </summary>
public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(PlayerId id, CancellationToken cancellationToken = default);
    Task<Player?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Player?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Player>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Player>> GetTopByStatisticAsync(
        string statistic, 
        int count = 10, 
        CancellationToken cancellationToken = default);
    Task AddAsync(Player player, CancellationToken cancellationToken = default);
    Task UpdateAsync(Player player, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(PlayerId id, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
}
