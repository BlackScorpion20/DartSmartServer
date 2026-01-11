using System.Text;
using dotenv.net;
using DartSmart.Infrastructure;
using DartSmart.WebApi.Endpoints;
using DartSmart.WebApi.Hubs;
using DartSmart.WebApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Load .env file
DotEnv.Load(options: new DotEnvOptions(
    envFilePaths: new[] { ".env", "../.env", "../../.env" },
    ignoreExceptions: true
));

// Configure Serilog
var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "DartSmartServer")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/dartsmart-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq(seqUrl ?? "http://localhost:5341")
    .CreateLogger();

try
{
    Log.Information("Starting DartSmartServer...");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Get configuration from environment variables
    var dbHost = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "localhost";
    var dbPort = Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "5432";
    var dbName = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "dartsmart";
    var dbUser = Environment.GetEnvironmentVariable("DATABASE_USER") ?? "dartsmart";
    var dbPassword = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "dartsmart_dev";
    var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

    var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? "DartSmartDefaultKeyForDevelopmentOnly123456789";
    var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "DartSmart";
    var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "DartSmart";
    var jwtExpiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") ?? "60");

    // Add configuration values for other services to use
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    builder.Configuration["Jwt:Key"] = jwtKey;
    builder.Configuration["Jwt:Issuer"] = jwtIssuer;
    builder.Configuration["Jwt:Audience"] = jwtAudience;
    builder.Configuration["Jwt:ExpiryMinutes"] = jwtExpiryMinutes.ToString();

    // Add Infrastructure (DbContext, Repositories, Services, Mediator)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Global Exception Handler
    builder.Services.AddTransient<GlobalExceptionHandler>();

    // OpenAPI / Swagger
    builder.Services.AddOpenApi();

    // SignalR
    builder.Services.AddSignalR();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // JWT Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };

            // Allow JWT in SignalR query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    
                    if (!string.IsNullOrEmpty(accessToken) && 
                        (path.StartsWithSegments("/hubs/game") || path.StartsWithSegments("/hubs/lobby")))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    // Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    // Global exception handler
    app.UseMiddleware<GlobalExceptionHandler>();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();

    // Map API Endpoints
    app.MapAuthEndpoints();
    app.MapGameEndpoints();
    app.MapStatsEndpoints();
    app.MapLobbyEndpoints();

    // Map SignalR Hubs
    app.MapHub<GameHub>("/hubs/game");
    app.MapHub<LobbyHub>("/hubs/lobby");

    // Health check
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
       .WithName("HealthCheck")
       .WithTags("System");

    Log.Information("DartSmartServer started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
