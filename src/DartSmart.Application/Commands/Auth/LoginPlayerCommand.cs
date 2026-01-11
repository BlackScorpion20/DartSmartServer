using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.Application.Commands.Auth;

public record LoginPlayerCommand(string Email, string Password) 
    : IRequest<Result<AuthResultDto>>;
