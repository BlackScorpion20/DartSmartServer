using DartSmart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DartSmart.Infrastructure.Persistence;

public class DartSmartDbContext : DbContext
{
    public DartSmartDbContext(DbContextOptions<DartSmartDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();
    public DbSet<DartThrow> DartThrows => Set<DartThrow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DartSmartDbContext).Assembly);
    }
}
