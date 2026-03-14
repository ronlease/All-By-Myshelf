namespace AllByMyshelf.Api.Features.Statistics;

/// <summary>
/// Statistics for the board games collection.
/// </summary>
public record BoardGameStatisticsDto
{
    /// <summary>Breakdown of board games by genre/category.</summary>
    public required IReadOnlyList<BreakdownItemDto> GenreBreakdown { get; init; }

    /// <summary>Total number of board games owned.</summary>
    public required int TotalCount { get; init; }
}
