namespace AllByMyshelf.Api.Features.Statistics;

/// <summary>
/// Combined statistics for records and books, returned by GET /api/v1/statistics.
/// </summary>
public record UnifiedStatisticsDto
{
    /// <summary>Books collection statistics.</summary>
    public required BookStatisticsDto Books { get; init; }

    /// <summary>Records collection statistics.</summary>
    public required RecordStatisticsDto Records { get; init; }
}
