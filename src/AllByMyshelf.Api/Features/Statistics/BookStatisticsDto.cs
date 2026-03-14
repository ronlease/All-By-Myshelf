namespace AllByMyshelf.Api.Features.Statistics;

/// <summary>
/// Statistics for the books collection.
/// </summary>
public record BookStatisticsDto
{
    /// <summary>Breakdown of books by author.</summary>
    public required IReadOnlyList<BreakdownItemDto> AuthorBreakdown { get; init; }

    /// <summary>Breakdown of books by decade published (e.g., "2000s", "2010s").</summary>
    public required IReadOnlyList<BreakdownItemDto> DecadeBreakdown { get; init; }

    /// <summary>Breakdown of books by genre.</summary>
    public required IReadOnlyList<BreakdownItemDto> GenreBreakdown { get; init; }

    /// <summary>Total number of books read.</summary>
    public required int TotalCount { get; init; }
}
