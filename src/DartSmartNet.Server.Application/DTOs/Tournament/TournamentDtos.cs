using System;
using System.Collections.Generic;
using DartSmartNet.Server.Domain.Enums;

namespace DartSmartNet.Server.Application.DTOs.Tournament;

// ============ Response DTOs ============

public record TournamentDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OrganizerId,
    string OrganizerUsername,
    TournamentFormat Format,
    TournamentStatus Status,
    GameType GameType,
    int StartingScore,
    int LegsToWin,
    int SetsToWin,
    int MaxParticipants,
    int CurrentParticipants,
    bool IsPublic,
    DateTime CreatedAt,
    DateTime? ScheduledStartAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    Guid? WinnerId,
    string? WinnerUsername
);

public record TournamentDetailDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OrganizerId,
    string OrganizerUsername,
    TournamentFormat Format,
    TournamentStatus Status,
    GameType GameType,
    int StartingScore,
    int LegsToWin,
    int SetsToWin,
    int MaxParticipants,
    bool IsPublic,
    string? JoinCode,
    DateTime CreatedAt,
    DateTime? ScheduledStartAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    Guid? WinnerId,
    string? WinnerUsername,
    List<TournamentParticipantDto> Participants,
    List<TournamentMatchDto> Matches
);

public record TournamentParticipantDto(
    Guid Id,
    Guid UserId,
    string Username,
    int? Seed,
    int? FinalPlacement,
    bool IsEliminated,
    int MatchesWon,
    int MatchesLost,
    int LegsWon,
    int LegsLost,
    DateTime JoinedAt
);

public record TournamentMatchDto(
    Guid Id,
    int Round,
    int MatchNumber,
    bool IsLosersBracket,
    Guid? Player1Id,
    string? Player1Username,
    Guid? Player2Id,
    string? Player2Username,
    Guid? WinnerId,
    string? WinnerUsername,
    Guid? GameSessionId,
    TournamentMatchStatus Status,
    int Player1Legs,
    int Player2Legs,
    int Player1Sets,
    int Player2Sets,
    Guid? NextMatchId,
    Guid? LoserNextMatchId,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

// ============ Request DTOs ============

public record CreateTournamentRequest(
    string Name,
    string? Description,
    TournamentFormat Format,
    GameType GameType,
    int StartingScore,
    int MaxParticipants,
    bool IsPublic = true,
    int LegsToWin = 3,
    int SetsToWin = 1,
    DateTime? ScheduledStartAt = null
);

public record UpdateTournamentRequest(
    string? Name,
    string? Description,
    DateTime? ScheduledStartAt
);

public record JoinTournamentRequest(
    string? JoinCode
);

public record TournamentBracketDto(
    Guid TournamentId,
    string TournamentName,
    TournamentFormat Format,
    int TotalRounds,
    List<TournamentRoundDto> Rounds
);

public record TournamentRoundDto(
    int RoundNumber,
    string RoundName,
    bool IsLosersBracket,
    List<TournamentMatchDto> Matches
);
