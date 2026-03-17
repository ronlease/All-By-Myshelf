using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations;

/// <inheritdoc />
public partial class MultipleCreators : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── releases ────────────────────────────────────────────────────────
        migrationBuilder.Sql("""
            ALTER TABLE releases ADD COLUMN artists text[] NOT NULL DEFAULT '{}';
            UPDATE releases SET artists = ARRAY[artist];
            ALTER TABLE releases DROP COLUMN artist;
            CREATE INDEX ix_releases_artists ON releases USING GIN (artists);
            """);

        // ── books ───────────────────────────────────────────────────────────
        migrationBuilder.Sql("""
            ALTER TABLE books ADD COLUMN authors text[] NOT NULL DEFAULT '{}';
            UPDATE books SET authors = CASE WHEN author IS NOT NULL THEN ARRAY[author] ELSE '{}' END;
            ALTER TABLE books DROP COLUMN author;
            CREATE INDEX ix_books_authors ON books USING GIN (authors);
            """);

        // ── board_games ─────────────────────────────────────────────────────
        migrationBuilder.Sql("""
            ALTER TABLE board_games ADD COLUMN designers text[] NOT NULL DEFAULT '{}';
            UPDATE board_games SET designers = CASE WHEN designer IS NOT NULL THEN ARRAY[designer] ELSE '{}' END;
            ALTER TABLE board_games DROP COLUMN designer;
            CREATE INDEX ix_board_games_designers ON board_games USING GIN (designers);
            """);

        // ── wantlist_releases ───────────────────────────────────────────────
        migrationBuilder.Sql("""
            ALTER TABLE wantlist_releases ADD COLUMN artists text[] NOT NULL DEFAULT '{}';
            UPDATE wantlist_releases SET artists = ARRAY[artist];
            ALTER TABLE wantlist_releases DROP COLUMN artist;
            CREATE INDEX ix_wantlist_releases_artists ON wantlist_releases USING GIN (artists);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ── releases ────────────────────────────────────────────────────────
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS ix_releases_artists;
            ALTER TABLE releases ADD COLUMN artist varchar(500) NOT NULL DEFAULT '';
            UPDATE releases SET artist = COALESCE(artists[1], '');
            ALTER TABLE releases DROP COLUMN artists;
            """);

        // ── books ───────────────────────────────────────────────────────────
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS ix_books_authors;
            ALTER TABLE books ADD COLUMN author varchar(500);
            UPDATE books SET author = authors[1];
            ALTER TABLE books DROP COLUMN authors;
            """);

        // ── board_games ─────────────────────────────────────────────────────
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS ix_board_games_designers;
            ALTER TABLE board_games ADD COLUMN designer varchar(500);
            UPDATE board_games SET designer = designers[1];
            ALTER TABLE board_games DROP COLUMN designers;
            """);

        // ── wantlist_releases ───────────────────────────────────────────────
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS ix_wantlist_releases_artists;
            ALTER TABLE wantlist_releases ADD COLUMN artist varchar(500) NOT NULL DEFAULT '';
            UPDATE wantlist_releases SET artist = COALESCE(artists[1], '');
            ALTER TABLE wantlist_releases DROP COLUMN artists;
            """);
    }
}
