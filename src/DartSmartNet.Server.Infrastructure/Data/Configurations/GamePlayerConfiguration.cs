using DartSmartNet.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmartNet.Server.Infrastructure.Data.Configurations;

public class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        builder.ToTable("game_players");

        builder.HasKey(gp => gp.Id);
        builder.Property(gp => gp.Id).HasColumnName("id");

        builder.Property(gp => gp.GameId)
            .HasColumnName("game_id")
            .IsRequired();

        builder.Property(gp => gp.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(gp => gp.PlayerOrder)
            .HasColumnName("player_order")
            .IsRequired();

        builder.Property(gp => gp.FinalScore)
            .HasColumnName("final_score");

        builder.Property(gp => gp.DartsThrown)
            .HasColumnName("darts_thrown")
            .HasDefaultValue(0);

        builder.Property(gp => gp.PointsScored)
            .HasColumnName("points_scored")
            .HasDefaultValue(0);

        builder.Property(gp => gp.PPD)
            .HasColumnName("ppd")
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.Property(gp => gp.IsWinner)
            .HasColumnName("is_winner")
            .HasDefaultValue(false);

        builder.Property(gp => gp.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique constraint
        builder.HasIndex(gp => new { gp.GameId, gp.UserId })
            .IsUnique();

        // Relationships
        builder.HasOne(gp => gp.User)
            .WithMany()
            .HasForeignKey(gp => gp.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
