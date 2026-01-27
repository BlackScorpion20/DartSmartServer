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
            .IsRequired(false);  // Optional for guest/bot players

        builder.Property(gp => gp.PlayerType)
            .HasColumnName("player_type")
            .HasDefaultValue(Domain.Enums.PlayerType.Human)
            .IsRequired();

        builder.Property(gp => gp.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(50)
            .IsRequired(false);

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

        // Unique constraint - allow multiple guests/bots (null UserId) per game
        builder.HasIndex(gp => new { gp.GameId, gp.UserId })
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");  // Only enforce uniqueness for real users

        // Relationships - optional for guest/bot players
        builder.HasOne(gp => gp.User)
            .WithMany()
            .HasForeignKey(gp => gp.UserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

