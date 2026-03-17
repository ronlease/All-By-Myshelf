namespace AllByMyshelf.Api.Features.Bgg;

/// <summary>
/// Public representation of a board game for API list responses.
/// </summary>
/// <param name="BggId">BGG game ID.</param>
/// <param name="Designers">List of designer names.</param>
/// <param name="Genre">Primary category/genre; null when not available.</param>
/// <param name="Id">Application-generated GUID.</param>
/// <param name="MaxPlayers">Maximum number of players; null when not available.</param>
/// <param name="MinPlayers">Minimum number of players; null when not available.</param>
/// <param name="ThumbnailUrl">Thumbnail image URL; null when not available.</param>
/// <param name="Title">Board game title.</param>
/// <param name="YearPublished">Year published; null when not available.</param>
public record BoardGameDto(
    int BggId,
    List<string> Designers,
    string? Genre,
    Guid Id,
    int? MaxPlayers,
    int? MinPlayers,
    string? ThumbnailUrl,
    string Title,
    int? YearPublished);
