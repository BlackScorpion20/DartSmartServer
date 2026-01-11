using DartSmart.Domain.Common;
using DartSmart.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmart.Infrastructure.Persistence.Configurations;

public class DartThrowConfiguration : IEntityTypeConfiguration<DartThrow>
{
    public void Configure(EntityTypeBuilder<DartThrow> builder)
    {
        builder.ToTable("dart_throws");

        builder.HasKey(dt => dt.Id);

        builder.Property(dt => dt.Id)
            .HasConversion(
                id => id.Value,
                value => DartThrowId.From(value))
            .HasColumnName("id");

        builder.Property(dt => dt.GameId)
            .HasConversion(
                id => id.Value,
                value => GameId.From(value))
            .HasColumnName("game_id");

        builder.Property(dt => dt.PlayerId)
            .HasConversion(
                id => id.Value,
                value => PlayerId.From(value))
            .HasColumnName("player_id");

        builder.Property(dt => dt.Segment)
            .HasColumnName("segment");

        builder.Property(dt => dt.Multiplier)
            .HasColumnName("multiplier");

        builder.Property(dt => dt.Points)
            .HasColumnName("points");

        builder.Property(dt => dt.Round)
            .HasColumnName("round");

        builder.Property(dt => dt.DartNumber)
            .HasColumnName("dart_number");

        builder.Property(dt => dt.Timestamp)
            .HasColumnName("timestamp");

        builder.Property(dt => dt.IsBust)
            .HasColumnName("is_bust");

        // Index for TimescaleDB hypertable
        builder.HasIndex(dt => dt.Timestamp);
        builder.HasIndex(dt => dt.PlayerId);
        builder.HasIndex(dt => dt.GameId);

        builder.Ignore(dt => dt.DomainEvents);
    }
}
