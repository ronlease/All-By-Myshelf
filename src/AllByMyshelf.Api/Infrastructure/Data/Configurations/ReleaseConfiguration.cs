using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AllByMyshelf.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Release"/> entity.
/// </summary>
public class ReleaseConfiguration : IEntityTypeConfiguration<Release>
{
    public void Configure(EntityTypeBuilder<Release> builder)
    {
        builder.ToTable("releases");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(r => r.DiscogsId)
            .HasColumnName("discogs_id")
            .IsRequired();

        builder.HasIndex(r => r.DiscogsId)
            .IsUnique()
            .HasDatabaseName("ix_releases_discogs_id");

        builder.Property(r => r.Artist)
            .HasColumnName("artist")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.Year)
            .HasColumnName("year");

        builder.Property(r => r.Format)
            .HasColumnName("format")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Label)
            .HasColumnName("label")
            .HasMaxLength(500);

        builder.Property(r => r.Country)
            .HasColumnName("country")
            .HasMaxLength(100);

        builder.Property(r => r.Genre)
            .HasColumnName("genre")
            .HasMaxLength(200);

        builder.Property(r => r.Notes)
            .HasColumnName("notes")
            .HasMaxLength(5000);

        builder.Property(r => r.Styles)
            .HasColumnName("styles")
            .HasMaxLength(1000);

        builder.Property(r => r.LastSyncedAt)
            .HasColumnName("last_synced_at")
            .IsRequired();
    }
}
