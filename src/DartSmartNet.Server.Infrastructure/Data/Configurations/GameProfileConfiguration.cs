using DartSmartNet.Server.Domain.Entities;
using DartSmartNet.Server.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmartNet.Server.Infrastructure.Data.Configurations;

public class GameProfileConfiguration : IEntityTypeConfiguration<GameProfile>
{
    public void Configure(EntityTypeBuilder<GameProfile> builder)
    {
        builder.ToTable("game_profiles");

        builder.HasKey(p => p.ProfileId);

        builder.Property(p => p.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.Property(p => p.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(p => p.GameType)
            .HasColumnName("game_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.StartingScore)
            .HasColumnName("starting_score")
            .IsRequired();

        builder.Property(p => p.OutMode)
            .HasColumnName("out_mode")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.InMode)
            .HasColumnName("in_mode")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.IsPublic)
            .HasColumnName("is_public")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(p => p.ExtensionSettings)
            .HasColumnName("extension_settings")
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(p => p.OwnerId)
            .HasDatabaseName("ix_game_profiles_owner_id");

        builder.HasIndex(p => p.IsPublic)
            .HasDatabaseName("ix_game_profiles_is_public");

        // Foreign key
        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
