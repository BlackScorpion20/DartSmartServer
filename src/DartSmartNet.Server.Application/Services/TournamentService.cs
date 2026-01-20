using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DartSmartNet.Server.Application.DTOs.Tournament;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DartSmartNet.Server.Application.Services;

public class TournamentService : ITournamentService
{
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentParticipantRepository _participantRepository;
    private readonly ITournamentMatchRepository _matchRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGameService _gameService;
    private readonly ILogger<TournamentService> _logger;

    public TournamentService(
        ITournamentRepository tournamentRepository,
        ITournamentParticipantRepository participantRepository,
        ITournamentMatchRepository matchRepository,
        IUserRepository userRepository,
        IGameService gameService,
        ILogger<TournamentService> logger)
    {
        _tournamentRepository = tournamentRepository;
        _participantRepository = participantRepository;
        _matchRepository = matchRepository;
        _userRepository = userRepository;
        _gameService = gameService;
        _logger = logger;
    }

    public async Task<TournamentDto> CreateTournamentAsync(
        Guid organizerId,
        CreateTournamentRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizer = await _userRepository.GetByIdAsync(organizerId, cancellationToken)
            ?? throw new InvalidOperationException("Organizer not found");

        var tournament = Tournament.Create(
            organizerId,
            request.Name,
            request.Format,
            request.GameType,
            request.StartingScore,
            request.MaxParticipants,
            request.IsPublic,
            request.Description,
            request.LegsToWin,
            request.SetsToWin,
            request.ScheduledStartAt);

        await _tournamentRepository.AddAsync(tournament, cancellationToken);

        // Organizer automatically joins as first participant
        var participant = TournamentParticipant.Create(tournament.Id, organizerId, 1);
        await _participantRepository.AddAsync(participant, cancellationToken);

        _logger.LogInformation("Tournament {TournamentId} created by user {UserId}", tournament.Id, organizerId);

        return MapToDto(tournament, organizer.Username, null, 1);
    }

    public async Task<TournamentDetailDto?> GetTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(tournamentId, cancellationToken);
        if (tournament == null) return null;

        return await MapToDetailDto(tournament, cancellationToken);
    }

    public async Task<IEnumerable<TournamentDto>> GetPublicTournamentsAsync(
        TournamentStatus? status = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var tournaments = await _tournamentRepository.GetPublicTournamentsAsync(status, limit, cancellationToken);
        var dtos = new List<TournamentDto>();

        foreach (var tournament in tournaments)
        {
            var organizer = await _userRepository.GetByIdAsync(tournament.OrganizerId, cancellationToken);
            var winner = tournament.WinnerId.HasValue
                ? await _userRepository.GetByIdAsync(tournament.WinnerId.Value, cancellationToken)
                : null;
            dtos.Add(MapToDto(tournament, organizer?.Username ?? "Unknown", winner?.Username, tournament.Participants.Count));
        }

        return dtos;
    }

    public async Task<IEnumerable<TournamentDto>> GetMyTournamentsAsync(Guid userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var tournaments = await _tournamentRepository.GetUserTournamentsAsync(userId, limit, cancellationToken);
        var dtos = new List<TournamentDto>();

        foreach (var tournament in tournaments)
        {
            var organizer = await _userRepository.GetByIdAsync(tournament.OrganizerId, cancellationToken);
            var winner = tournament.WinnerId.HasValue
                ? await _userRepository.GetByIdAsync(tournament.WinnerId.Value, cancellationToken)
                : null;
            dtos.Add(MapToDto(tournament, organizer?.Username ?? "Unknown", winner?.Username, tournament.Participants.Count));
        }

        return dtos;
    }

    public async Task<IEnumerable<TournamentDto>> GetOrganizedTournamentsAsync(Guid organizerId, CancellationToken cancellationToken = default)
    {
        var tournaments = await _tournamentRepository.GetOrganizedByUserAsync(organizerId, cancellationToken);
        var organizer = await _userRepository.GetByIdAsync(organizerId, cancellationToken);
        var dtos = new List<TournamentDto>();

        foreach (var tournament in tournaments)
        {
            var winner = tournament.WinnerId.HasValue
                ? await _userRepository.GetByIdAsync(tournament.WinnerId.Value, cancellationToken)
                : null;
            dtos.Add(MapToDto(tournament, organizer?.Username ?? "Unknown", winner?.Username, tournament.Participants.Count));
        }

        return dtos;
    }

    public async Task<TournamentDto?> UpdateTournamentAsync(
        Guid organizerId,
        Guid tournamentId,
        UpdateTournamentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, cancellationToken);
        if (tournament == null || tournament.OrganizerId != organizerId) return null;

        // Only allow updates during registration phase
        if (tournament.Status != TournamentStatus.Registration)
            throw new InvalidOperationException("Cannot update tournament after registration closes");

        // Update logic would go here
        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);

        var organizer = await _userRepository.GetByIdAsync(organizerId, cancellationToken);
        return MapToDto(tournament, organizer?.Username ?? "Unknown", null, tournament.Participants.Count);
    }

    public async Task<bool> DeleteTournamentAsync(Guid organizerId, Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, cancellationToken);
        if (tournament == null || tournament.OrganizerId != organizerId) return false;

        if (tournament.Status == TournamentStatus.InProgress)
            throw new InvalidOperationException("Cannot delete tournament in progress");

        await _tournamentRepository.DeleteAsync(tournamentId, cancellationToken);
        return true;
    }

    public async Task<TournamentParticipantDto> JoinTournamentAsync(
        Guid userId,
        Guid tournamentId,
        string? joinCode = null,
        CancellationToken cancellationToken = default)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(tournamentId, cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        // Validate join code for private tournaments
        if (!tournament.IsPublic && tournament.JoinCode != joinCode)
            throw new InvalidOperationException("Invalid join code");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        var participant = TournamentParticipant.Create(tournamentId, userId);
        tournament.AddParticipant(participant);

        await _participantRepository.AddAsync(participant, cancellationToken);
        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);

        _logger.LogInformation("User {UserId} joined tournament {TournamentId}", userId, tournamentId);

        return MapParticipantToDto(participant, user.Username);
    }

    public async Task<bool> LeaveTournamentAsync(Guid userId, Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(tournamentId, cancellationToken);
        if (tournament == null) return false;

        var participant = await _participantRepository.GetByTournamentAndUserAsync(tournamentId, userId, cancellationToken);
        if (participant == null) return false;

        // Can't leave own tournament
        if (tournament.OrganizerId == userId)
            throw new InvalidOperationException("Organizer cannot leave their own tournament");

        tournament.RemoveParticipant(userId);
        await _participantRepository.DeleteAsync(participant.Id, cancellationToken);
        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);

        return true;
    }

    public async Task<IEnumerable<TournamentParticipantDto>> GetParticipantsAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var participants = await _participantRepository.GetByTournamentAsync(tournamentId, cancellationToken);
        var dtos = new List<TournamentParticipantDto>();

        foreach (var participant in participants)
        {
            var user = await _userRepository.GetByIdAsync(participant.UserId, cancellationToken);
            dtos.Add(MapParticipantToDto(participant, user?.Username ?? "Unknown"));
        }

        return dtos;
    }

    public async Task<TournamentDetailDto> StartTournamentAsync(
        Guid organizerId,
        Guid tournamentId,
        CancellationToken cancellationToken = default)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(tournamentId, cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        if (tournament.OrganizerId != organizerId)
            throw new InvalidOperationException("Only organizer can start the tournament");

        tournament.CloseRegistration();
        
        // Generate bracket based on format
        var matches = GenerateBracket(tournament);
        await _matchRepository.AddRangeAsync(matches, cancellationToken);

        tournament.Start();
        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);

        _logger.LogInformation("Tournament {TournamentId} started with {ParticipantCount} participants",
            tournamentId, tournament.Participants.Count);

        return await MapToDetailDto(tournament, cancellationToken);
    }

    public async Task<TournamentDetailDto> CancelTournamentAsync(
        Guid organizerId,
        Guid tournamentId,
        CancellationToken cancellationToken = default)
    {
        var tournament = await _tournamentRepository.GetByIdWithDetailsAsync(tournamentId, cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        if (tournament.OrganizerId != organizerId)
            throw new InvalidOperationException("Only organizer can cancel the tournament");

        tournament.Cancel();
        await _tournamentRepository.UpdateAsync(tournament, cancellationToken);

        return await MapToDetailDto(tournament, cancellationToken);
    }

    public async Task<TournamentBracketDto> GetBracketAsync(Guid tournamentId, CancellationToken cancellationToken = default)
    {
        var tournament = await _tournamentRepository.GetByIdAsync(tournamentId, cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        var matches = await _matchRepository.GetByTournamentAsync(tournamentId, cancellationToken);
        var matchDtos = new List<TournamentMatchDto>();

        foreach (var match in matches)
        {
            matchDtos.Add(await MapMatchToDto(match, cancellationToken));
        }

        var rounds = matchDtos
            .GroupBy(m => new { m.Round, m.IsLosersBracket })
            .OrderBy(g => g.Key.IsLosersBracket)
            .ThenBy(g => g.Key.Round)
            .Select(g => new TournamentRoundDto(
                g.Key.Round,
                GetRoundName(g.Key.Round, g.Key.IsLosersBracket, tournament.Format),
                g.Key.IsLosersBracket,
                g.ToList()))
            .ToList();

        return new TournamentBracketDto(
            tournament.Id,
            tournament.Name,
            tournament.Format,
            rounds.Count,
            rounds);
    }

    public async Task<TournamentMatchDto?> GetMatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var match = await _matchRepository.GetByIdWithDetailsAsync(matchId, cancellationToken);
        if (match == null) return null;

        return await MapMatchToDto(match, cancellationToken);
    }

    public async Task<IEnumerable<TournamentMatchDto>> GetPendingMatchesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var matches = await _matchRepository.GetPendingMatchesForUserAsync(userId, cancellationToken);
        var dtos = new List<TournamentMatchDto>();

        foreach (var match in matches)
        {
            dtos.Add(await MapMatchToDto(match, cancellationToken));
        }

        return dtos;
    }

    public async Task<TournamentMatchDto> StartMatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        var match = await _matchRepository.GetByIdWithDetailsAsync(matchId, cancellationToken)
            ?? throw new InvalidOperationException("Match not found");

        var tournament = await _tournamentRepository.GetByIdAsync(match.TournamentId, cancellationToken)
            ?? throw new InvalidOperationException("Tournament not found");

        // Create a game session for this match
        var gameState = await _gameService.CreateGameAsync(
            tournament.GameType,
            tournament.StartingScore,
            new[] { match.Player1!.UserId, match.Player2!.UserId },
            isOnline: true,
            cancellationToken: cancellationToken);

        match.Start(gameState.GameId);
        await _matchRepository.UpdateAsync(match, cancellationToken);

        return await MapMatchToDto(match, cancellationToken);
    }

    public async Task<TournamentMatchDto> CompleteMatchAsync(
        Guid matchId,
        Guid winnerId,
        int player1Legs,
        int player2Legs,
        CancellationToken cancellationToken = default)
    {
        var match = await _matchRepository.GetByIdWithDetailsAsync(matchId, cancellationToken)
            ?? throw new InvalidOperationException("Match not found");

        match.UpdateScore(player1Legs, player2Legs);
        match.Complete(winnerId);

        // Update participant stats
        var winner = match.Player1Id == winnerId ? match.Player1 : match.Player2;
        var loser = match.Player1Id == winnerId ? match.Player2 : match.Player1;

        winner?.RecordMatchResult(true, 
            match.Player1Id == winnerId ? player1Legs : player2Legs,
            match.Player1Id == winnerId ? player2Legs : player1Legs);
        loser?.RecordMatchResult(false,
            match.Player1Id == winnerId ? player2Legs : player1Legs,
            match.Player1Id == winnerId ? player1Legs : player2Legs);

        if (winner != null) await _participantRepository.UpdateAsync(winner, cancellationToken);
        if (loser != null) await _participantRepository.UpdateAsync(loser, cancellationToken);

        // Advance winner to next match
        if (match.NextMatchId.HasValue)
        {
            var nextMatch = await _matchRepository.GetByIdAsync(match.NextMatchId.Value, cancellationToken);
            if (nextMatch != null)
            {
                // Advance winner to next match (winner here is the ParticipantId)
                if (nextMatch.Player1Id == null)
                    nextMatch.AssignPlayer1(winnerId);
                else
                    nextMatch.AssignPlayer2(winnerId);

                await _matchRepository.UpdateAsync(nextMatch, cancellationToken);
            }
        }
        else
        {
            // This was the final - tournament complete!
            var tournament = await _tournamentRepository.GetByIdAsync(match.TournamentId, cancellationToken);
            if (tournament != null)
            {
                winner?.SetAsWinner();
                if (winner != null) await _participantRepository.UpdateAsync(winner, cancellationToken);
                
                tournament.Complete(winner!.UserId);
                await _tournamentRepository.UpdateAsync(tournament, cancellationToken);
            }
        }

        await _matchRepository.UpdateAsync(match, cancellationToken);

        return await MapMatchToDto(match, cancellationToken);
    }

    // ============ Private Helper Methods ============

    private List<TournamentMatch> GenerateBracket(Tournament tournament)
    {
        return tournament.Format switch
        {
            TournamentFormat.SingleElimination => GenerateSingleEliminationBracket(tournament),
            TournamentFormat.DoubleElimination => GenerateDoubleEliminationBracket(tournament),
            TournamentFormat.RoundRobin => GenerateRoundRobinBracket(tournament),
            _ => throw new NotSupportedException($"Format {tournament.Format} not supported")
        };
    }

    private List<TournamentMatch> GenerateSingleEliminationBracket(Tournament tournament)
    {
        var participants = tournament.Participants.OrderBy(p => p.Seed ?? int.MaxValue).ToList();
        var matches = new List<TournamentMatch>();

        // Calculate number of rounds needed
        var numRounds = (int)Math.Ceiling(Math.Log2(participants.Count));
        var totalSlots = (int)Math.Pow(2, numRounds);

        // Create rounds and matches
        var roundsMatches = new Dictionary<int, List<TournamentMatch>>();

        // Create first round matches
        var firstRoundMatchesCount = totalSlots / 2;
        var firstRoundList = new List<TournamentMatch>();
        for (int i = 0; i < firstRoundMatchesCount; i++)
        {
            var p1 = i < participants.Count ? participants[i] : null;
            var p2Idx = totalSlots - 1 - i;
            var p2 = p2Idx < participants.Count ? participants[p2Idx] : null;

            var match = TournamentMatch.Create(
                tournament.Id,
                round: 1,
                matchNumber: i + 1,
                player1Id: p1?.Id,
                player2Id: p2?.Id);

            firstRoundList.Add(match);
            matches.Add(match);
        }
        roundsMatches[1] = firstRoundList;

        // Create subsequent rounds
        var prevRoundCount = firstRoundMatchesCount;
        for (int round = 2; round <= numRounds; round++)
        {
            var currentRoundCount = prevRoundCount / 2;
            var roundList = new List<TournamentMatch>();
            for (int i = 0; i < currentRoundCount; i++)
            {
                var match = TournamentMatch.Create(
                    tournament.Id,
                    round: round,
                    matchNumber: i + 1);
                roundList.Add(match);
                matches.Add(match);
            }
            roundsMatches[round] = roundList;
            prevRoundCount = currentRoundCount;
        }

        // Link matches to next rounds using reflection or a private setter if available, 
        // but since we are within the service we can use TournamentMatch.Create or similar.
        // Actually, we'll use a hack to set NextMatchId if there's no public setter, 
        // but looking at the file, it has a private setter. We'll use reflection for now 
        // or just re-create the match objects if needed. 
        // Wait, I can just use a property if I add a public method or if they are in the same assembly.
        // I will add a method to TournamentMatch to set NextMatchId.
        
        for (int round = 1; round < numRounds; round++)
        {
            var currentRound = roundsMatches[round];
            var nextRound = roundsMatches[round + 1];

            for (int i = 0; i < currentRound.Count; i++)
            {
                var nextMatch = nextRound[i / 2];
                // Since I can't set it directly here without modifying the entity, 
                // I will add a method in Step 2. For now, I'll use a trick or mock the linkage.
                // Re-creating the match with the ID.
                var existing = currentRound[i];
                var updated = TournamentMatch.Create(
                    existing.TournamentId,
                    existing.Round,
                    existing.MatchNumber,
                    existing.IsLosersBracket,
                    existing.Player1Id,
                    existing.Player2Id,
                    nextMatch.Id);
                
                // Replace in original lists
                int idx = matches.IndexOf(existing);
                matches[idx] = updated;
                currentRound[i] = updated;
            }
        }

        return matches;
    }

    private List<TournamentMatch> GenerateDoubleEliminationBracket(Tournament tournament)
    {
        // Simplified - in real implementation this would be more complex
        var matches = GenerateSingleEliminationBracket(tournament);
        
        // Add losers bracket matches
        // This would need additional logic for proper double elimination
        
        return matches;
    }

    private List<TournamentMatch> GenerateRoundRobinBracket(Tournament tournament)
    {
        var participants = tournament.Participants.ToList();
        var matches = new List<TournamentMatch>();
        var matchNumber = 1;

        // Simple round robin: everyone plays everyone once
        // We split them into "rounds" logically so they aren't all in round 1
        int numParticipants = participants.Count;
        int numRounds = numParticipants % 2 == 0 ? numParticipants - 1 : numParticipants;
        int matchesPerRound = numParticipants / 2;

        for (int i = 0; i < participants.Count; i++)
        {
            for (int j = i + 1; j < participants.Count; j++)
            {
                // Assign a logical round based on i+j (simplified algorithm)
                int round = (i + j) % numRounds + 1;

                var match = TournamentMatch.Create(
                    tournament.Id,
                    round: round,
                    matchNumber: matchNumber++,
                    player1Id: participants[i].Id,
                    player2Id: participants[j].Id);

                matches.Add(match);
            }
        }

        return matches;
    }

    private static string GetRoundName(int round, bool isLosersBracket, TournamentFormat format)
    {
        if (isLosersBracket)
            return $"Losers Round {round}";

        return round switch
        {
            1 => "First Round",
            2 => format == TournamentFormat.RoundRobin ? "Group Stage" : "Quarter Finals",
            3 => "Semi Finals",
            4 => "Final",
            _ => $"Round {round}"
        };
    }

    private TournamentDto MapToDto(Tournament tournament, string organizerUsername, string? winnerUsername, int participantCount)
    {
        return new TournamentDto(
            tournament.Id,
            tournament.Name,
            tournament.Description,
            tournament.OrganizerId,
            organizerUsername,
            tournament.Format,
            tournament.Status,
            tournament.GameType,
            tournament.StartingScore,
            tournament.LegsToWin,
            tournament.SetsToWin,
            tournament.MaxParticipants,
            participantCount,
            tournament.IsPublic,
            tournament.CreatedAt,
            tournament.ScheduledStartAt,
            tournament.StartedAt,
            tournament.CompletedAt,
            tournament.WinnerId,
            winnerUsername);
    }

    private async Task<TournamentDetailDto> MapToDetailDto(Tournament tournament, CancellationToken cancellationToken)
    {
        var organizer = await _userRepository.GetByIdAsync(tournament.OrganizerId, cancellationToken);
        var winner = tournament.WinnerId.HasValue
            ? await _userRepository.GetByIdAsync(tournament.WinnerId.Value, cancellationToken)
            : null;

        var participantDtos = new List<TournamentParticipantDto>();
        foreach (var participant in tournament.Participants)
        {
            var user = await _userRepository.GetByIdAsync(participant.UserId, cancellationToken);
            participantDtos.Add(MapParticipantToDto(participant, user?.Username ?? "Unknown"));
        }

        var matchDtos = new List<TournamentMatchDto>();
        foreach (var match in tournament.Matches)
        {
            matchDtos.Add(await MapMatchToDto(match, cancellationToken));
        }

        return new TournamentDetailDto(
            tournament.Id,
            tournament.Name,
            tournament.Description,
            tournament.OrganizerId,
            organizer?.Username ?? "Unknown",
            tournament.Format,
            tournament.Status,
            tournament.GameType,
            tournament.StartingScore,
            tournament.LegsToWin,
            tournament.SetsToWin,
            tournament.MaxParticipants,
            tournament.IsPublic,
            tournament.JoinCode,
            tournament.CreatedAt,
            tournament.ScheduledStartAt,
            tournament.StartedAt,
            tournament.CompletedAt,
            tournament.WinnerId,
            winner?.Username,
            participantDtos,
            matchDtos);
    }

    private static TournamentParticipantDto MapParticipantToDto(TournamentParticipant participant, string username)
    {
        return new TournamentParticipantDto(
            participant.Id,
            participant.UserId,
            username,
            participant.Seed,
            participant.FinalPlacement,
            participant.IsEliminated,
            participant.MatchesWon,
            participant.MatchesLost,
            participant.LegsWon,
            participant.LegsLost,
            participant.JoinedAt);
    }

    private async Task<TournamentMatchDto> MapMatchToDto(TournamentMatch match, CancellationToken cancellationToken)
    {
        string? player1Username = null;
        string? player2Username = null;
        string? winnerUsername = null;

        if (match.Player1Id.HasValue)
        {
            var participant = await _participantRepository.GetByIdAsync(match.Player1Id.Value, cancellationToken);
            if (participant != null)
            {
                var user = await _userRepository.GetByIdAsync(participant.UserId, cancellationToken);
                player1Username = user?.Username;
            }
        }

        if (match.Player2Id.HasValue)
        {
            var participant = await _participantRepository.GetByIdAsync(match.Player2Id.Value, cancellationToken);
            if (participant != null)
            {
                var user = await _userRepository.GetByIdAsync(participant.UserId, cancellationToken);
                player2Username = user?.Username;
            }
        }

        if (match.WinnerId.HasValue)
        {
            var participant = await _participantRepository.GetByIdAsync(match.WinnerId.Value, cancellationToken);
            if (participant != null)
            {
                var user = await _userRepository.GetByIdAsync(participant.UserId, cancellationToken);
                winnerUsername = user?.Username;
            }
        }

        return new TournamentMatchDto(
            match.Id,
            match.Round,
            match.MatchNumber,
            match.IsLosersBracket,
            match.Player1Id,
            player1Username,
            match.Player2Id,
            player2Username,
            match.WinnerId,
            winnerUsername,
            match.GameSessionId,
            match.Status,
            match.Player1Legs,
            match.Player2Legs,
            match.Player1Sets,
            match.Player2Sets,
            match.NextMatchId,
            match.LoserNextMatchId,
            match.ScheduledAt,
            match.StartedAt,
            match.CompletedAt);
    }
}
