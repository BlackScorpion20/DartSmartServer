namespace DartSmart.Application.DTOs;

public record PlayerDto(
    string Id,
    string Username,
    string Email,
    DateTime CreatedAt,
    PlayerStatisticsDto Statistics
);

public record PlayerSummaryDto(
    string Id,
    string Username,
    decimal Average3Dart
);
