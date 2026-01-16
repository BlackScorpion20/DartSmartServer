using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.Interfaces;

public interface ITournamentRepository
{
    Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tournament?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tournament>> GetPublicTournamentsAsync(TournamentStatus? status = null, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tournament>> GetUserTournamentsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<Tournament>> GetOrganizedByUserAsync(Guid organizerId, CancellationToken cancellationToken = default);
    Task<Tournament?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task AddAsync(Tournament tournament, CancellationToken cancellationToken = default);
    Task UpdateAsync(Tournament tournament, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ITournamentParticipantRepository
{
    Task<TournamentParticipant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TournamentParticipant?> GetByTournamentAndUserAsync(Guid tournamentId, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TournamentParticipant>> GetByTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task AddAsync(TournamentParticipant participant, CancellationToken cancellationToken = default);
    Task UpdateAsync(TournamentParticipant participant, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ITournamentMatchRepository
{
    Task<TournamentMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TournamentMatch?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TournamentMatch>> GetByTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TournamentMatch>> GetByRoundAsync(Guid tournamentId, int round, CancellationToken cancellationToken = default);
    Task<IEnumerable<TournamentMatch>> GetPendingMatchesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(TournamentMatch match, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<TournamentMatch> matches, CancellationToken cancellationToken = default);
    Task UpdateAsync(TournamentMatch match, CancellationToken cancellationToken = default);
}
