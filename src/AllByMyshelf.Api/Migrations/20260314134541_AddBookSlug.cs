using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "slug",
                table: "books",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "slug",
                table: "books");
        }
    }
}
