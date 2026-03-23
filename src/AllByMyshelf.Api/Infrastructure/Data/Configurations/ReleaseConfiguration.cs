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

        builder.Property(r => r.AddedAt)
            .HasColumnName("added_at");

        builder.Property(r => r.Artists)
            .HasColumnName("artists")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(r => r.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasMaxLength(2000);

        builder.Property(r => r.Title)
            .HasColumnName("title")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.ThumbnailUrl)
            .HasColumnName("thumbnail_url")
            .HasMaxLength(2000);

        builder.Property(r => r.Year)
            .HasColumnName("year");

        builder.Property(r => r.Format)
            .HasColumnName("format")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Genre)
            .HasColumnName("genre")
            .HasMaxLength(200);

        builder.Property(r => r.HighestPrice)
            .HasColumnName("highest_price")
            .HasPrecision(10, 2);

        builder.Property(r => r.LowestPrice)
            .HasColumnName("lowest_price")
            .HasPrecision(10, 2);

        builder.Property(r => r.MedianPrice)
            .HasColumnName("median_price")
            .HasPrecision(10, 2);

        builder.Property(r => r.Notes)
            .HasColumnName("notes")
            .HasMaxLength(2000);

        builder.Property(r => r.Rating)
            .HasColumnName("rating");

        builder.Property(r => r.LastSyncedAt)
            .HasColumnName("last_synced_at")
            .IsRequired();

        builder.Property(r => r.TrackArtists)
            .HasColumnName("track_artists")
            .HasColumnType("text[]");
    }
}
