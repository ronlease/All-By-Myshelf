namespace AllByMyshelf.Api.Features.BoardGameGeek;

/// <summary>
/// Filter criteria for querying board games.
/// All string filters use case-insensitive contains matching.
/// </summary>
/// <param name="Designer">Optional designer filter.</param>
/// <param name="Genre">Optional genre/category filter.</param>
/// <param name="PlayerCount">Optional player count filter (game must support this number of players).</param>
/// <param name="Title">Optional title filter.</param>
/// <param name="Year">Optional year filter.</param>
public record BoardGameFilter(
    string? Designer = null,
    string? Genre = null,
    int? PlayerCount = null,
    string? Title = null,
    string? Year = null);
