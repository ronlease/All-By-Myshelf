using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReleaseMarketplacePricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "highest_price",
                table: "releases",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lowest_price",
                table: "releases",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "median_price",
                table: "releases",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "highest_price", table: "releases");
            migrationBuilder.DropColumn(name: "lowest_price", table: "releases");
            migrationBuilder.DropColumn(name: "median_price", table: "releases");
        }
    }
}
