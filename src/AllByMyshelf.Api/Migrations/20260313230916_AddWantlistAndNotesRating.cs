using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWantlistAndNotesRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "releases",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rating",
                table: "releases",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "wantlist_releases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    artist = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                name: "wantlist_releases");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "releases");

            migrationBuilder.DropColumn(
                name: "rating",
                table: "releases");
        }
    }
}
