namespace AllByMyshelf.Api.Features.Statistics;

/// <summary>
/// A single category in a breakdown (e.g., a genre or decade with its count).
/// </summary>
public record BreakdownItemDto
{
    /// <summary>Number of items in this category.</summary>
    public required int Count { get; init; }

    /// <summary>Category label (e.g., "Rock", "1990s").</summary>
    public required string Label { get; init; }
}
