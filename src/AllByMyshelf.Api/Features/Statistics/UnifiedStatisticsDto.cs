namespace AllByMyshelf.Api.Features.Statistics;

/// <summary>
/// Combined statistics for records, books, and board games, returned by GET /api/v1/statistics.
/// </summary>
public record UnifiedStatisticsDto
{
    /// <summary>Board games collection statistics.</summary>
    public required BoardGameStatisticsDto BoardGames { get; init; }

    /// <summary>Books collection statistics.</summary>
    public required BookStatisticsDto Books { get; init; }

    /// <summary>Records collection statistics.</summary>
    public required RecordStatisticsDto Records { get; init; }
}
