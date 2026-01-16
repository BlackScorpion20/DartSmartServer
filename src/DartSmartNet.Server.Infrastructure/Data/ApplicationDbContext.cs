using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;

namespace DartSmartNet.Server.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<PlayerStats> PlayerStats => Set<PlayerStats>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();
    public DbSet<DartThrow> DartThrows => Set<DartThrow>();
    public DbSet<Bot> Bots => Set<Bot>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TrainingSession> TrainingSessions => Set<TrainingSession>();
    public DbSet<TrainingThrow> TrainingThrows => Set<TrainingThrow>();
    public DbSet<GameEventLog> GameEventLogs => Set<GameEventLog>();
    public DbSet<GameProfile> GameProfiles => Set<GameProfile>();
    
    // Tournament entities
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();
    public DbSet<TournamentMatch> TournamentMatches => Set<TournamentMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
