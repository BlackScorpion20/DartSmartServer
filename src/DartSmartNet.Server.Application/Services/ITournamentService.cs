using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Tournament;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.Services;

public interface ITournamentService
{
    // Tournament CRUD
    Task<TournamentDto> CreateTournamentAsync(Guid organizerId, CreateTournamentRequest request, CancellationToken cancellationToken = default);
    Task<TournamentDetailDto?> GetTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TournamentDto>> GetPublicTournamentsAsync(TournamentStatus? status = null, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<TournamentDto>> GetMyTournamentsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<TournamentDto>> GetOrganizedTournamentsAsync(Guid organizerId, CancellationToken cancellationToken = default);
    Task<TournamentDto?> UpdateTournamentAsync(Guid organizerId, Guid tournamentId, UpdateTournamentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteTournamentAsync(Guid organizerId, Guid tournamentId, CancellationToken cancellationToken = default);
    
    // Participation
    Task<TournamentParticipantDto> JoinTournamentAsync(Guid userId, Guid tournamentId, string? joinCode = null, CancellationToken cancellationToken = default);
    Task<bool> LeaveTournamentAsync(Guid userId, Guid tournamentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TournamentParticipantDto>> GetParticipantsAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    
    // Tournament lifecycle
    Task<TournamentDetailDto> StartTournamentAsync(Guid organizerId, Guid tournamentId, CancellationToken cancellationToken = default);
    Task<TournamentDetailDto> CancelTournamentAsync(Guid organizerId, Guid tournamentId, CancellationToken cancellationToken = default);
    
    // Bracket
    Task<TournamentBracketDto> GetBracketAsync(Guid tournamentId, CancellationToken cancellationToken = default);
    
    // Match management
    Task<TournamentMatchDto?> GetMatchAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TournamentMatchDto>> GetPendingMatchesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TournamentMatchDto> StartMatchAsync(Guid matchId, CancellationToken cancellationToken = default);
    Task<TournamentMatchDto> CompleteMatchAsync(Guid matchId, Guid winnerId, int player1Legs, int player2Legs, CancellationToken cancellationToken = default);
}
