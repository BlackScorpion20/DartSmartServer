using DartSmart.Application.Common;
using DartSmart.Application.Interfaces;
using DartSmart.Infrastructure.Persistence;
using DartSmart.Infrastructure.Persistence.Repositories;
using DartSmart.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DartSmart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<DartSmartDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IDartThrowRepository, DartThrowRepository>();
        services.AddScoped<ILobbyRepository, LobbyRepository>();

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();

        // Mediator
        services.AddMediator(typeof(DartSmart.Application.Common.IMediator));

        return services;
    }
}
