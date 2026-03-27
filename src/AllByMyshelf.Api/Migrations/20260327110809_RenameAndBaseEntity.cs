using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameAndBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "bgg_id",
                table: "board_games",
                newName: "board_game_geek_id");

            migrationBuilder.RenameIndex(
                name: "ix_board_games_bgg_id",
                table: "board_games",
                newName: "ix_board_games_board_game_geek_id");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "wantlist_releases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "releases",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "books",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "board_games",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "wantlist_releases");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "releases");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "books");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "board_games");

            migrationBuilder.RenameColumn(
                name: "board_game_geek_id",
                table: "board_games",
                newName: "bgg_id");

            migrationBuilder.RenameIndex(
                name: "ix_board_games_board_game_geek_id",
                table: "board_games",
                newName: "ix_board_games_bgg_id");
        }
    }
}
