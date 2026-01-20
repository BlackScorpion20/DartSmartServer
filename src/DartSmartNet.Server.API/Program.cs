using System;
using System.Threading.Tasks;
using DartSmartNet.Server.API.Hubs;
using DartSmartNet.Server.Application.Interfaces;
using DartSmartNet.Server.Application.Services;
using DartSmartNet.Server.Infrastructure.AI;
using DartSmartNet.Server.Infrastructure.Extensions;
using DartSmartNet.Server.Infrastructure.Authentication;
using DartSmartNet.Server.Infrastructure.Data;
using DartSmartNet.Server.Infrastructure.Repositories;
using InfraServices = DartSmartNet.Server.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure JWT Settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings not configured");
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
    }));

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        // Allow SignalR to use JWT from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];

                // If the request is for our hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5000",
                "http://localhost:5001",
                "https://localhost:5001",
                "http://localhost:3000" // For future web client
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddScoped<IStatsRepository, StatsRepository>();
builder.Services.AddScoped<IBotRepository, BotRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITrainingRepository, TrainingRepository>();
builder.Services.AddScoped<IGameProfileRepository, GameProfileRepository>();

// Tournament Repositories
builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
builder.Services.AddScoped<ITournamentParticipantRepository, TournamentParticipantRepository>();
builder.Services.AddScoped<ITournamentMatchRepository, TournamentMatchRepository>();

// Services
builder.Services.AddScoped<IAuthService, InfraServices.AuthService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<ITrainingService, TrainingService>();
builder.Services.AddScoped<IGameProfileService, GameProfileService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddSingleton<IMatchmakingService, MatchmakingService>();
builder.Services.AddScoped<IBotService, BotEngine>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// Event Broadcasting System
builder.Services.AddSingleton<IGameEventBroadcaster, InfraServices.GameEventBroadcaster>();

// Game Extensions (Plugin Architecture)
builder.Services.AddScoped<IGameExtension, StatisticsExtension>();
builder.Services.AddScoped<IGameExtension, EventLoggingExtension>();
builder.Services.AddScoped<IGameExtension, WebSocketBroadcastExtension>();

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DartSmartNet Server API v1");
        options.RoutePrefix = string.Empty; // Swagger UI at root
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR Hubs
app.MapHub<GameHub>("/hubs/game");
app.MapHub<BroadcastHub>("/hubs/broadcast"); // For external clients (LED, overlays, etc.)

// Health Check Endpoint
app.MapHealthChecks("/health");

// Register extensions with broadcaster
using (var scope = app.Services.CreateScope())
{
    var broadcaster = scope.ServiceProvider.GetRequiredService<IGameEventBroadcaster>();
    var extensions = scope.ServiceProvider.GetServices<IGameExtension>();

    foreach (var extension in extensions)
    {
        broadcaster.RegisterExtension(extension);
    }
}

app.Run();
