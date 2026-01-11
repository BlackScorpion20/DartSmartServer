namespace DartSmart.Application.DTOs;

public record LobbyPlayerDto(
    string PlayerId,
    string Username,
    decimal Average3Dart,
    DateTime JoinedAt
);

public record MatchFoundDto(
    string GameId,
    List<LobbyPlayerDto> Players
);
