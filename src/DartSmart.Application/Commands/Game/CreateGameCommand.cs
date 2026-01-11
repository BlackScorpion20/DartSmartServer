using DartSmart.Application.Common;
using DartSmart.Application.DTOs;
using DartSmart.Domain.ValueObjects;

namespace DartSmart.Application.Commands.Game;

public record CreateGameCommand(
    string PlayerId,
    GameType GameType, 
    int StartScore,
    X01InMode InMode,
    X01OutMode OutMode) : IRequest<Result<GameDto>>;
