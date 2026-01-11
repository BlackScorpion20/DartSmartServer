using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Commands.Game;

public record JoinGameCommand(string PlayerId, string GameId) : IRequest<Result<GameDto>>;
