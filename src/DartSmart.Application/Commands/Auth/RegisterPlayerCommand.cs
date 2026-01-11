using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Commands.Auth;

public record RegisterPlayerCommand(string Username, string Email, string Password) 
    : IRequest<Result<AuthResultDto>>;
