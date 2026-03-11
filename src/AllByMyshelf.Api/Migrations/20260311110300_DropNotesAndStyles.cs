using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations
{
    /// <inheritdoc />
    public partial class DropNotesAndStyles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "notes",
                table: "releases");

            migrationBuilder.DropColumn(
                name: "styles",
                table: "releases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "releases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "styles",
                table: "releases",
                type: "text",
                nullable: true);
        }
    }
}
