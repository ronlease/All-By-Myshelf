using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AllByMyshelf.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Book"/> entity.
/// </summary>
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.HardcoverId)
            .HasColumnName("hardcover_id")
            .IsRequired();

        builder.HasIndex(b => b.HardcoverId)
            .IsUnique()
            .HasDatabaseName("ix_books_hardcover_id");

        builder.Property(b => b.Authors)
            .HasColumnName("authors")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(b => b.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasMaxLength(2000);

        builder.Property(b => b.Genre)
            .HasColumnName("genre")
            .HasMaxLength(200);

        builder.Property(b => b.LastSyncedAt)
            .HasColumnName("last_synced_at")
            .IsRequired();

        builder.Property(b => b.Slug)
            .HasColumnName("slug")
            .HasMaxLength(500);

        builder.Property(b => b.Title)
            .HasColumnName("title")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(b => b.Year)
            .HasColumnName("year");
    }
}
