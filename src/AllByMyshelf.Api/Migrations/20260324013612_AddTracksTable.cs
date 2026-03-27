using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTracksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "ix_tracks_release_id",
                table: "tracks",
                column: "release_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tracks");
        }
    }
}
