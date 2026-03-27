using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AllByMyshelf.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Track"/> entity.
/// </summary>
public class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("tracks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(t => t.Artists)
            .HasColumnName("artists")
            .HasColumnType("text[]");

        builder.Property(t => t.Position)
            .HasColumnName("position")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.ReleaseId)
            .HasColumnName("release_id")
            .IsRequired();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne<Release>()
            .WithMany(r => r.Tracks)
            .HasForeignKey(t => t.ReleaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.ReleaseId)
            .HasDatabaseName("ix_tracks_release_id");
    }
}
