using System;

namespace DartSmartNet.Server.Application.DTOs.Stats;

/// <summary>
/// Comprehensive player statistics DTO
/// </summary>
public sealed record PlayerStatsDto(
    Guid UserId,
    string Username,
    
    // Basic Stats
    int GamesPlayed,
    int GamesWon,
    int GamesLost,
    decimal WinRate,
    decimal AveragePPD,
    decimal ThreeDartAverage,
    
    // High Scores
    int HighestCheckout,
    int HighestScore,
    int Total180s,
    int Total171s,
    int Total140s,
    int Total100Plus,
    
    // Checkout Statistics
    int TotalCheckouts,
    decimal CheckoutPercentage,
    
    // Session Averages
    decimal BestSessionAverage,
    decimal WorstSessionAverage,
    decimal First9Average,
    
    // Streaks
    int CurrentWinStreak,
    int LongestWinStreak,
    int CurrentLossStreak,
    int LongestLossStreak,
    
    // Legs Statistics
    int TotalLegsPlayed,
    int TotalLegsWon,
    decimal LegsWinRate,
    
    // Time-based
    DateTime? LastGameAt
);

/// <summary>
/// Compact stats for leaderboards
/// </summary>
public sealed record LeaderboardEntryDto(
    int Rank,
    Guid UserId,
    string Username,
    int GamesPlayed,
    int GamesWon,
    decimal WinRate,
    decimal ThreeDartAverage,
    int HighestCheckout,
    int Total180s
);

/// <summary>
/// Stats summary for quick display
/// </summary>
public sealed record StatsSummaryDto(
    int TotalGames,
    int Wins,
    int Losses,
    decimal WinRate,
    decimal AverageScore,
    int Best180Count,
    int HighestCheckout,
    string Trend // "up", "down", "stable"
);
