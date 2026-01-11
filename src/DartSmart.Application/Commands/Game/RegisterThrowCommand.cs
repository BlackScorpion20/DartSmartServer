using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Commands.Game;

public record RegisterThrowCommand(
    string PlayerId,
    string GameId,
    int Segment,
    int Multiplier,
    int DartNumber) : IRequest<Result<DartThrowDto>>;
