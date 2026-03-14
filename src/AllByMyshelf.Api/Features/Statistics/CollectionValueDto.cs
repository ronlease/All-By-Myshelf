namespace AllByMyshelf.Api.Features.Statistics;

/// <summary>
/// Aggregate collection value data returned by GET /api/v1/statistics/collection-value.
/// </summary>
public record CollectionValueDto
{
    /// <summary>Number of releases excluded from the value calculation.</summary>
    public required int ExcludedCount { get; init; }

    /// <summary>Number of releases included in the value calculation.</summary>
    public required int IncludedCount { get; init; }

    /// <summary>Total estimated value of the collection.</summary>
    public required decimal TotalValue { get; init; }
}
