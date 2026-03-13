namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Optional filter criteria for the random release query.
/// <see cref="Decade"/>, if provided, must be in the form "1980s".
/// </summary>
public record RandomReleaseFilter(
    string? Decade = null,
    string? Format = null,
    string? Genre = null
);
