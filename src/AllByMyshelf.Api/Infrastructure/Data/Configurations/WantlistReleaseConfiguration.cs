using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AllByMyshelf.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="WantlistRelease"/> entity.
/// </summary>
public class WantlistReleaseConfiguration : IEntityTypeConfiguration<WantlistRelease>
{
    public void Configure(EntityTypeBuilder<WantlistRelease> builder)
    {
        builder.ToTable("wantlist_releases");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(w => w.AddedAt)
            .HasColumnName("added_at");

        builder.Property(w => w.Artists)
            .HasColumnName("artists")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(w => w.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasMaxLength(2000);

        builder.Property(w => w.DiscogsId)
            .HasColumnName("discogs_id")
            .IsRequired();

        builder.HasIndex(w => w.DiscogsId)
            .IsUnique()
            .HasDatabaseName("ix_wantlist_releases_discogs_id");

        builder.Property(w => w.Format)
            .HasColumnName("format")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.Genre)
            .HasColumnName("genre")
            .HasMaxLength(200);

        builder.Property(w => w.LastSyncedAt)
            .HasColumnName("last_synced_at")
            .IsRequired();

        builder.Property(w => w.ThumbnailUrl)
            .HasColumnName("thumbnail_url")
            .HasMaxLength(2000);

        builder.Property(w => w.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(w => w.Year)
            .HasColumnName("year");
    }
}
