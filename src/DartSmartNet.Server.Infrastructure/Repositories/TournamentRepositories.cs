using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using DartSmartNet.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DartSmartNet.Server.Infrastructure.Repositories;

public class TournamentRepository : ITournamentRepository
{
    private readonly ApplicationDbContext _context;

    public TournamentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tournaments
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tournament?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Matches)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Tournament>> GetPublicTournamentsAsync(
        TournamentStatus? status = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tournaments
            .Where(t => t.IsPublic)
            .Include(t => t.Participants);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value).Include(t => t.Participants);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tournament>> GetUserTournamentsAsync(
        Guid userId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tournaments
            .Include(t => t.Participants)
            .Where(t => t.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Tournament>> GetOrganizedByUserAsync(
        Guid organizerId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tournaments
            .Include(t => t.Participants)
            .Where(t => t.OrganizerId == organizerId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tournament?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        return await _context.Tournaments
            .FirstOrDefaultAsync(t => t.JoinCode == joinCode, cancellationToken);
    }

    public async Task AddAsync(Tournament tournament, CancellationToken cancellationToken = default)
    {
        await _context.Tournaments.AddAsync(tournament, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Tournament tournament, CancellationToken cancellationToken = default)
    {
        _context.Tournaments.Update(tournament);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tournament = await _context.Tournaments.FindAsync(new object[] { id }, cancellationToken);
        if (tournament != null)
        {
            _context.Tournaments.Remove(tournament);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class TournamentParticipantRepository : ITournamentParticipantRepository
{
    private readonly ApplicationDbContext _context;

    public TournamentParticipantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TournamentParticipant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TournamentParticipants
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<TournamentParticipant?> GetByTournamentAndUserAsync(
        Guid tournamentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TournamentParticipants
            .FirstOrDefaultAsync(p => p.TournamentId == tournamentId && p.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<TournamentParticipant>> GetByTournamentAsync(
        Guid tournamentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TournamentParticipants
            .Where(p => p.TournamentId == tournamentId)
            .OrderBy(p => p.Seed ?? int.MaxValue)
            .ThenBy(p => p.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TournamentParticipant participant, CancellationToken cancellationToken = default)
    {
        await _context.TournamentParticipants.AddAsync(participant, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TournamentParticipant participant, CancellationToken cancellationToken = default)
    {
        _context.TournamentParticipants.Update(participant);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var participant = await _context.TournamentParticipants.FindAsync(new object[] { id }, cancellationToken);
        if (participant != null)
        {
            _context.TournamentParticipants.Remove(participant);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class TournamentMatchRepository : ITournamentMatchRepository
{
    private readonly ApplicationDbContext _context;

    public TournamentMatchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TournamentMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TournamentMatches
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<TournamentMatch?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TournamentMatches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Include(m => m.GameSession)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<TournamentMatch>> GetByTournamentAsync(
        Guid tournamentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TournamentMatches
            .Where(m => m.TournamentId == tournamentId)
            .OrderBy(m => m.IsLosersBracket)
            .ThenBy(m => m.Round)
            .ThenBy(m => m.MatchNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TournamentMatch>> GetByRoundAsync(
        Guid tournamentId,
        int round,
        CancellationToken cancellationToken = default)
    {
        return await _context.TournamentMatches
            .Where(m => m.TournamentId == tournamentId && m.Round == round)
            .OrderBy(m => m.MatchNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<TournamentMatch>> GetPendingMatchesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var participantIds = await _context.TournamentParticipants
            .Where(p => p.UserId == userId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        return await _context.TournamentMatches
            .Include(m => m.Tournament)
            .Where(m => (m.Status == TournamentMatchStatus.Ready || m.Status == TournamentMatchStatus.Pending) 
                        && (participantIds.Contains(m.Player1Id!.Value) || participantIds.Contains(m.Player2Id!.Value)))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TournamentMatch match, CancellationToken cancellationToken = default)
    {
        await _context.TournamentMatches.AddAsync(match, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<TournamentMatch> matches, CancellationToken cancellationToken = default)
    {
        await _context.TournamentMatches.AddRangeAsync(matches, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TournamentMatch match, CancellationToken cancellationToken = default)
    {
        _context.TournamentMatches.Update(match);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
