using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "books",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cover_image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    genre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    hardcover_id = table.Column<int>(type: "integer", nullable: false),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    title = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_books", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_books_hardcover_id",
                table: "books",
                column: "hardcover_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "books");
        }
    }
}
