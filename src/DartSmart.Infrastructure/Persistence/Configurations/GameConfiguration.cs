using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using DartSmart.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmart.Infrastructure.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("games");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasConversion(
                id => id.Value,
                value => GameId.From(value))
            .HasColumnName("id");

        builder.Property(g => g.GameType)
            .HasColumnName("game_type")
            .HasConversion<string>();

        builder.Property(g => g.StartScore)
            .HasColumnName("start_score");

        builder.Property(g => g.InMode)
            .HasColumnName("in_mode")
            .HasConversion<string>();

        builder.Property(g => g.OutMode)
            .HasColumnName("out_mode")
            .HasConversion<string>();

        builder.Property(g => g.Status)
            .HasColumnName("status")
            .HasConversion<string>();

        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(g => g.FinishedAt)
            .HasColumnName("finished_at");

        builder.Property(g => g.WinnerId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? PlayerId.From(value.Value) : null)
            .HasColumnName("winner_id");

        builder.Property(g => g.CurrentPlayerIndex)
            .HasColumnName("current_player_index");

        builder.Property(g => g.CurrentRound)
            .HasColumnName("current_round");

        builder.HasMany(g => g.Players)
            .WithOne()
            .HasForeignKey(gp => gp.GameId);

        builder.HasMany(g => g.Throws)
            .WithOne()
            .HasForeignKey(t => t.GameId);

        builder.Ignore(g => g.DomainEvents);
    }
}
