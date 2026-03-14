namespace AllByMyshelf.Api.Features.Statistics;

/// <summary>
/// Statistics for the vinyl/records collection.
/// </summary>
public record RecordStatisticsDto
{
    /// <summary>Breakdown of records by decade (e.g., "1970s", "1980s").</summary>
    public required IReadOnlyList<BreakdownItemDto> DecadeBreakdown { get; init; }

    /// <summary>Number of records excluded from value calculation (no pricing data).</summary>
    public required int ExcludedFromValueCount { get; init; }

    /// <summary>Breakdown of records by format (e.g., "LP", "CD").</summary>
    public required IReadOnlyList<BreakdownItemDto> FormatBreakdown { get; init; }

    /// <summary>Breakdown of records by genre.</summary>
    public required IReadOnlyList<BreakdownItemDto> GenreBreakdown { get; init; }

    /// <summary>Total number of records in the collection.</summary>
    public required int TotalCount { get; init; }

    /// <summary>Total estimated value based on lowest marketplace prices (USD).</summary>
    public required decimal TotalValue { get; init; }
}
