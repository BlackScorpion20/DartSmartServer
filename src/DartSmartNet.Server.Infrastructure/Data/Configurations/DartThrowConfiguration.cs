using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmartNet.Server.Infrastructure.Data.Configurations;

public class DartThrowConfiguration : IEntityTypeConfiguration<DartThrow>
{
    public void Configure(EntityTypeBuilder<DartThrow> builder)
    {
        builder.ToTable("dart_throws");

        builder.HasKey(dt => dt.Id);
        builder.Property(dt => dt.Id).HasColumnName("id");

        builder.Property(dt => dt.GameId)
            .HasColumnName("game_id")
            .IsRequired();

        builder.Property(dt => dt.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(dt => dt.RoundNumber)
            .HasColumnName("round_number")
            .IsRequired();

        builder.Property(dt => dt.DartNumber)
            .HasColumnName("dart_number")
            .IsRequired();

        builder.Property(dt => dt.Segment)
            .HasColumnName("segment")
            .IsRequired();

        builder.Property(dt => dt.Multiplier)
            .HasColumnName("multiplier")
            .HasConversion(
                v => (int)v,
                v => (Multiplier)v)
            .IsRequired();

        builder.Property(dt => dt.Points)
            .HasColumnName("points")
            .IsRequired();

        builder.Property(dt => dt.ThrownAt)
            .HasColumnName("thrown_at")
            .IsRequired();

        builder.Property(dt => dt.RawData)
            .HasColumnName("raw_data");

        builder.Property(dt => dt.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Relationships
        builder.HasOne(dt => dt.User)
            .WithMany()
            .HasForeignKey(dt => dt.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(dt => new { dt.GameId, dt.UserId });
        builder.HasIndex(dt => new { dt.UserId, dt.ThrownAt });
    }
}
