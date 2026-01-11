using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Commands.Game;

public record CreateBotCommand(string Username, int SkillLevel) 
    : IRequest<Result<PlayerDto>>;
