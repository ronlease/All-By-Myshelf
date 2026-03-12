namespace AllByMyshelf.Api.Models.DTOs;

/// <summary>
/// Optional filter criteria for the releases collection query.
/// All string comparisons are case-insensitive contains matches.
/// </summary>
public record ReleaseFilter(
    string? Artist = null,
    string? Format = null,
    string? Genre = null,
    string? Search = null,
    string? Title = null,
    string? Year = null
);
