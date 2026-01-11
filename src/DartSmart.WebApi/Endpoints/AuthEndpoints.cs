using DartSmart.Application.Commands.Auth;
using DartSmart.Application.Common;
using DartSmart.Application.DTOs;

namespace DartSmart.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication");

        group.MapPost("/register", async (RegisterDto request, IMediator mediator) =>
        {
            var command = new RegisterPlayerCommand(request.Username, request.Email, request.Password);
            var result = await mediator.Send(command);

            return result.Match(
                success => Results.Ok(success),
                error => Results.BadRequest(new { error })
            );
        })
        .WithName("Register")
        .Produces<AuthResultDto>(200)
        .Produces(400);

        group.MapPost("/login", async (LoginDto request, IMediator mediator) =>
        {
            var command = new LoginPlayerCommand(request.Email, request.Password);
            var result = await mediator.Send(command);

            return result.Match(
                success => Results.Ok(success),
                error => Results.Unauthorized()
            );
        })
        .WithName("Login")
        .Produces<AuthResultDto>(200)
        .Produces(401);
    }
}
