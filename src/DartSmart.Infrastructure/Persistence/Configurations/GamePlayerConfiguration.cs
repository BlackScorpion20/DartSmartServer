using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmart.Infrastructure.Persistence.Configurations;

public class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        builder.ToTable("game_players");

        builder.HasKey(gp => new { GameId = gp.GameId, PlayerId = gp.PlayerId });

        builder.Property(gp => gp.GameId)
            .HasConversion(
                id => id.Value,
                value => GameId.From(value))
            .HasColumnName("game_id");

        builder.Property(gp => gp.PlayerId)
            .HasConversion(
                id => id.Value,
                value => PlayerId.From(value))
            .HasColumnName("player_id");

        builder.Property(gp => gp.CurrentScore)
            .HasColumnName("current_score");

        builder.Property(gp => gp.TurnOrder)
            .HasColumnName("turn_order");

        builder.Property(gp => gp.DartsThrown)
            .HasColumnName("darts_thrown");

        builder.Property(gp => gp.LegsWon)
            .HasColumnName("legs_won");

        builder.Property(gp => gp.SetsWon)
            .HasColumnName("sets_won");

        builder.Property(gp => gp.JoinedAt)
            .HasColumnName("joined_at");

        builder.Ignore(gp => gp.Id);
        builder.Ignore(gp => gp.DomainEvents);
    }
}
