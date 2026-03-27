using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "board_games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bgg_id = table.Column<int>(type: "integer", nullable: false),
                    cover_image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    designers = table.Column<List<string>>(type: "text[]", nullable: false),
                    genre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    max_players = table.Column<int>(type: "integer", nullable: true),
                    max_playtime = table.Column<int>(type: "integer", nullable: true),
                    min_players = table.Column<int>(type: "integer", nullable: true),
                    min_playtime = table.Column<int>(type: "integer", nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    year_published = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_board_games", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "books",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    authors = table.Column<List<string>>(type: "text[]", nullable: false),
                    cover_image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    genre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    hardcover_id = table.Column<int>(type: "integer", nullable: false),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_books", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "releases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    artists = table.Column<List<string>>(type: "text[]", nullable: false),
                    cover_image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    discogs_id = table.Column<int>(type: "integer", nullable: false),
                    format = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    genre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    highest_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lowest_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    median_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    track_artists = table.Column<List<string>>(type: "text[]", nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_releases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wantlist_releases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    artists = table.Column<List<string>>(type: "text[]", nullable: false),
                    cover_image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    discogs_id = table.Column<int>(type: "integer", nullable: false),
                    format = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    genre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    thumbnail_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wantlist_releases", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artists = table.Column<List<string>>(type: "text[]", nullable: false),
                    position = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    release_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracks", x => x.id);
                    table.ForeignKey(
                        name: "FK_tracks_releases_release_id",
                        column: x => x.release_id,
                        principalTable: "releases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_board_games_bgg_id",
                table: "board_games",
                column: "bgg_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_books_hardcover_id",
                table: "books",
                column: "hardcover_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_releases_discogs_id",
                table: "releases",
                column: "discogs_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tracks_release_id",
                table: "tracks",
                column: "release_id");

            migrationBuilder.CreateIndex(
                name: "ix_wantlist_releases_discogs_id",
                table: "wantlist_releases",
                column: "discogs_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");

            migrationBuilder.DropTable(
                name: "board_games");

            migrationBuilder.DropTable(
                name: "books");

            migrationBuilder.DropTable(
                name: "tracks");

            migrationBuilder.DropTable(
                name: "wantlist_releases");

            migrationBuilder.DropTable(
                name: "releases");
        }
    }
}
