using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmartNet.Server.Infrastructure.Data.Configurations;

public class BotConfiguration : IEntityTypeConfiguration<Bot>
{
    public void Configure(EntityTypeBuilder<Bot> builder)
    {
        builder.ToTable("bots");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).HasColumnName("id");

        builder.Property(b => b.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.Difficulty)
            .HasColumnName("difficulty")
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<BotDifficulty>(v))
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.AvgPPD)
            .HasColumnName("avg_ppd")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(b => b.ConsistencyFactor)
            .HasColumnName("consistency_factor")
            .HasPrecision(3, 2)
            .IsRequired();

        builder.Property(b => b.CheckoutSkill)
            .HasColumnName("checkout_skill")
            .HasPrecision(3, 2)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}
