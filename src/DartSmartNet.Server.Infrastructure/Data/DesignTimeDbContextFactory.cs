using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DartSmartNet.Server.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use a default connection string for migrations
        // This will be overridden at runtime by Program.cs
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=dartsmartnet;Username=dartuser;Password=dartpass");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
