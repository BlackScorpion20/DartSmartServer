using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Commands.Game;

public record StartGameCommand(string GameId) : IRequest<Result<GameDto>>;
