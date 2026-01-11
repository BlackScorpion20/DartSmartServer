using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmart.Infrastructure.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("players");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                value => PlayerId.From(value))
            .HasColumnName("id");

        builder.Property(p => p.Username)
            .HasColumnName("username")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.OwnsOne(p => p.Statistics, stats =>
        {
            stats.Property(s => s.TotalGames).HasColumnName("total_games");
            stats.Property(s => s.Wins).HasColumnName("wins");
            stats.Property(s => s.Best3DartScore).HasColumnName("best_3_dart_score");
            stats.Property(s => s.Count180s).HasColumnName("count_180s");
            stats.Property(s => s.HighestCheckout).HasColumnName("highest_checkout");
            stats.Property(s => s.TotalDarts).HasColumnName("total_darts");
            stats.Property(s => s.TotalPoints).HasColumnName("total_points");
        });

        builder.HasIndex(p => p.Email).IsUnique();
        builder.HasIndex(p => p.Username).IsUnique();

        builder.Ignore(p => p.DomainEvents);
    }
}
