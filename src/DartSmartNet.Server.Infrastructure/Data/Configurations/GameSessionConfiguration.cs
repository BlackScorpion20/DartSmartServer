using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmartNet.Server.Infrastructure.Data.Configurations;

public class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.ToTable("game_sessions");

        builder.HasKey(gs => gs.Id);
        builder.Property(gs => gs.Id).HasColumnName("id");

        builder.Property(gs => gs.GameType)
            .HasColumnName("game_type")
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<GameType>(v))
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(gs => gs.StartingScore)
            .HasColumnName("starting_score");

        builder.Property(gs => gs.Status)
            .HasColumnName("status")
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<GameStatus>(v))
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(GameStatus.WaitingForPlayers);

        builder.Property(gs => gs.StartedAt)
            .HasColumnName("started_at")
            .IsRequired();

        builder.Property(gs => gs.EndedAt)
            .HasColumnName("ended_at");

        builder.Property(gs => gs.WinnerId)
            .HasColumnName("winner_id");

        builder.Property(gs => gs.IsOnline)
            .HasColumnName("is_online")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(gs => gs.IsBotGame)
            .HasColumnName("is_bot_game")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(gs => gs.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Relationships
        builder.HasOne(gs => gs.Winner)
            .WithMany()
            .HasForeignKey(gs => gs.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(gs => gs.Players)
            .WithOne(gp => gp.Game)
            .HasForeignKey(gp => gp.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(gs => gs.Throws)
            .WithOne(dt => dt.Game)
            .HasForeignKey(dt => dt.GameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(gs => gs.Status);
        builder.HasIndex(gs => gs.WinnerId);
    }
}
