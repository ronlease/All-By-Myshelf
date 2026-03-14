namespace AllByMyshelf.Api.Features.Statistics;

/// <summary>
/// Statistics for the books collection.
/// </summary>
public record BookStatisticsDto
{
    /// <summary>Breakdown of books by genre.</summary>
    public required IReadOnlyList<BreakdownItemDto> GenreBreakdown { get; init; }

    /// <summary>Total number of books read.</summary>
    public required int TotalCount { get; init; }
}
