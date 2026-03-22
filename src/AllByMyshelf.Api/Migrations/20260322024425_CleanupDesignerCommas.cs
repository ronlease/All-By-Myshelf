using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AllByMyshelf.Api.Migrations;

/// <inheritdoc />
public partial class CleanupDesignerCommas : Migration
{
    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data cleanup is not reversible — comma-separated entries cannot be
        // reconstructed once split. This is intentionally a no-op.
    }

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Split any comma-separated designer names within a single array element
        // into separate array elements. For example:
        //   designers = ARRAY['Designer1, Designer2']
        // becomes:
        //   designers = ARRAY['Designer1', 'Designer2']
        migrationBuilder.Sql("""
            UPDATE board_games
            SET designers = (
                SELECT ARRAY_AGG(trimmed)
                FROM (
                    SELECT DISTINCT TRIM(elem) AS trimmed
                    FROM UNNEST(designers) AS raw_elem,
                         UNNEST(STRING_TO_ARRAY(raw_elem, ',')) AS elem
                    WHERE TRIM(elem) <> ''
                ) sub
            )
            WHERE EXISTS (
                SELECT 1 FROM UNNEST(designers) AS d WHERE d LIKE '%,%'
            );
            """);
    }
}
