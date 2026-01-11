namespace DartSmart.Application.DTOs;

public record LeaderboardEntryDto(
    int Rank,
    string PlayerId,
    string Username,
    decimal Value,
    string StatisticType
);

public record GlobalStatisticsDto(
    int TotalPlayers,
    int TotalGames,
    int TotalDartsThrown,
    int Total180s,
    decimal AverageGameDuration
);
