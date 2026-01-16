using DartSmartNet.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmartNet.Server.Infrastructure.Data.Configurations;

public class GameEventLogConfiguration : IEntityTypeConfiguration<GameEventLog>
{
    public void Configure(EntityTypeBuilder<GameEventLog> builder)
    {
        builder.ToTable("game_event_logs");

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.GameId)
            .HasColumnName("game_id")
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.EventData)
            .HasColumnName("event_data")
            .HasColumnType("jsonb") // PostgreSQL JSONB for efficient querying
            .IsRequired();

        builder.Property(e => e.PlayerUsername)
            .HasColumnName("player_username")
            .HasMaxLength(50);

        // Indexes for efficient querying
        builder.HasIndex(e => e.GameId)
            .HasDatabaseName("ix_game_event_logs_game_id");

        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("ix_game_event_logs_timestamp");

        builder.HasIndex(e => new { e.GameId, e.Timestamp })
            .HasDatabaseName("ix_game_event_logs_game_timestamp");

        // Foreign key
        builder.HasOne(e => e.GameSession)
            .WithMany()
            .HasForeignKey(e => e.GameId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
