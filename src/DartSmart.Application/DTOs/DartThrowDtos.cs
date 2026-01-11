namespace DartSmart.Application.DTOs;

public record DartThrowDto(
    string Id,
    string GameId,
    string PlayerId,
    int Segment,
    int Multiplier,
    int Points,
    int Round,
    int DartNumber,
    DateTime Timestamp,
    bool IsBust
);

public record RegisterThrowDto(
    string GameId,
    int Segment,
    int Multiplier,
    int DartNumber
);
