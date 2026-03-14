using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "board_games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bgg_id = table.Column<int>(type: "integer", nullable: false),
                    cover_image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    designer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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

            migrationBuilder.CreateIndex(
                name: "ix_board_games_bgg_id",
                table: "board_games",
                column: "bgg_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "board_games");
        }
    }
}
