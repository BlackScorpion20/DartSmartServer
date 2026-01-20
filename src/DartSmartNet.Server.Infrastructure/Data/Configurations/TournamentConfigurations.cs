using DartSmartNet.Server.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DartSmartNet.Server.Infrastructure.Data.Configurations;

public class TournamentConfiguration : IEntityTypeConfiguration<Tournament>
{
    public void Configure(EntityTypeBuilder<Tournament> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.JoinCode)
            .HasMaxLength(10);

        builder.HasOne(t => t.Organizer)
            .WithMany()
            .HasForeignKey(t => t.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Winner)
            .WithMany()
            .HasForeignKey(t => t.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Participants)
            .WithOne(p => p.Tournament)
            .HasForeignKey(p => p.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Matches)
            .WithOne(m => m.Tournament)
            .HasForeignKey(m => m.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.IsPublic);
        builder.HasIndex(t => t.JoinCode)
            .IsUnique()
            .HasFilter("\"JoinCode\" IS NOT NULL");
    }
}

public class TournamentParticipantConfiguration : IEntityTypeConfiguration<TournamentParticipant>
{
    public void Configure(EntityTypeBuilder<TournamentParticipant> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Tournament)
            .WithMany(t => t.Participants)
            .HasForeignKey(p => p.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one user per tournament
        builder.HasIndex(p => new { p.TournamentId, p.UserId })
            .IsUnique();

        builder.HasIndex(p => p.UserId);
    }
}

public class TournamentMatchConfiguration : IEntityTypeConfiguration<TournamentMatch>
{
    public void Configure(EntityTypeBuilder<TournamentMatch> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.Tournament)
            .WithMany(t => t.Matches)
            .HasForeignKey(m => m.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Player1)
            .WithMany()
            .HasForeignKey(m => m.Player1Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Player2)
            .WithMany()
            .HasForeignKey(m => m.Player2Id)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Winner)
            .WithMany()
            .HasForeignKey(m => m.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.GameSession)
            .WithMany()
            .HasForeignKey(m => m.GameSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.NextMatch)
            .WithMany()
            .HasForeignKey(m => m.NextMatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.LoserNextMatch)
            .WithMany()
            .HasForeignKey(m => m.LoserNextMatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.TournamentId, m.Round, m.MatchNumber });
        builder.HasIndex(m => m.Status);
    }
}
