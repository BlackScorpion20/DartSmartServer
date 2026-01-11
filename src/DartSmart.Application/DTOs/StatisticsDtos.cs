namespace DartSmart.Application.DTOs;

public record PlayerStatisticsDto(
    int TotalGames,
    int Wins,
    int Best3DartScore,
    int Count180s,
    int HighestCheckout,
    int TotalDarts,
    int TotalPoints,
    decimal WinRate,
    decimal AveragePerDart,
    decimal Average3Dart
);
