using AllByMyshelf.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AllByMyshelf.Api.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="BoardGame"/> entity.
/// </summary>
public class BoardGameConfiguration : IEntityTypeConfiguration<BoardGame>
{
    public void Configure(EntityTypeBuilder<BoardGame> builder)
    {
        builder.ToTable("board_games");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.BggId)
            .HasColumnName("bgg_id")
            .IsRequired();

        builder.HasIndex(b => b.BggId)
            .IsUnique()
            .HasDatabaseName("ix_board_games_bgg_id");

        builder.Property(b => b.CoverImageUrl)
            .HasColumnName("cover_image_url")
            .HasMaxLength(2000);

        builder.Property(b => b.Description)
            .HasColumnName("description")
            .HasMaxLength(10000);

        builder.Property(b => b.Designer)
            .HasColumnName("designer")
            .HasMaxLength(500);

        builder.Property(b => b.Genre)
            .HasColumnName("genre")
            .HasMaxLength(500);

        builder.Property(b => b.LastSyncedAt)
            .HasColumnName("last_synced_at")
            .IsRequired();

        builder.Property(b => b.MaxPlayers)
            .HasColumnName("max_players");

        builder.Property(b => b.MaxPlaytime)
            .HasColumnName("max_playtime");

        builder.Property(b => b.MinPlayers)
            .HasColumnName("min_players");

        builder.Property(b => b.MinPlaytime)
            .HasColumnName("min_playtime");

        builder.Property(b => b.ThumbnailUrl)
            .HasColumnName("thumbnail_url")
            .HasMaxLength(2000);

        builder.Property(b => b.Title)
            .HasColumnName("title")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(b => b.YearPublished)
            .HasColumnName("year_published");
    }
}
