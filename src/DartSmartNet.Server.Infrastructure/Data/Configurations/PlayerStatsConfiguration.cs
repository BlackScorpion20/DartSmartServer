using DartSmartNet.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmartNet.Server.Infrastructure.Data.Configurations;

public class PlayerStatsConfiguration : IEntityTypeConfiguration<PlayerStats>
{
    public void Configure(EntityTypeBuilder<PlayerStats> builder)
    {
        builder.ToTable("player_stats");

        builder.HasKey(ps => ps.Id);
        builder.Property(ps => ps.Id).HasColumnName("id");

        builder.Property(ps => ps.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(ps => ps.UserId)
            .IsUnique();

        builder.Property(ps => ps.GamesPlayed)
            .HasColumnName("games_played")
            .HasDefaultValue(0);

        builder.Property(ps => ps.GamesWon)
            .HasColumnName("games_won")
            .HasDefaultValue(0);

        builder.Property(ps => ps.GamesLost)
            .HasColumnName("games_lost")
            .HasDefaultValue(0);

        builder.Property(ps => ps.TotalDartsThrown)
            .HasColumnName("total_darts_thrown")
            .HasDefaultValue(0);

        builder.Property(ps => ps.TotalPointsScored)
            .HasColumnName("total_points_scored")
            .HasDefaultValue(0);

        builder.Property(ps => ps.AveragePPD)
            .HasColumnName("average_ppd")
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.Property(ps => ps.HighestCheckout)
            .HasColumnName("highest_checkout")
            .HasDefaultValue(0);

        builder.Property(ps => ps.Total180s)
            .HasColumnName("total_180s")
            .HasDefaultValue(0);

        builder.Property(ps => ps.Total171s)
            .HasColumnName("total_171s")
            .HasDefaultValue(0);

        builder.Property(ps => ps.Total140s)
            .HasColumnName("total_140s")
            .HasDefaultValue(0);

        builder.Property(ps => ps.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ps => ps.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Ignore computed property
        builder.Ignore(ps => ps.WinRate);
    }
}
