namespace AllByMyshelf.Api.Features.BoardGameGeek;

/// <summary>
/// Full detail representation of a single board game, returned by the GET /api/v1/boardgames/{id} endpoint.
/// </summary>
public class BoardGameDetailDto
{
    /// <summary>BoardGameGeek game ID.</summary>
    public int BoardGameGeekId { get; init; }

    /// <summary>Full cover image URL; null when not available.</summary>
    public string? CoverImageUrl { get; init; }

    /// <summary>Game description; null when not available.</summary>
    public string? Description { get; init; }

    /// <summary>List of designer names.</summary>
    public IReadOnlyList<string> Designers { get; init; } = [];

    /// <summary>Primary category/genre; null when not available.</summary>
    public string? Genre { get; init; }

    /// <summary>Application-generated identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Maximum number of players; null when not available.</summary>
    public int? MaxPlayers { get; init; }

    /// <summary>Maximum playtime in minutes; null when not available.</summary>
    public int? MaxPlaytime { get; init; }

    /// <summary>Minimum number of players; null when not available.</summary>
    public int? MinPlayers { get; init; }

    /// <summary>Minimum playtime in minutes; null when not available.</summary>
    public int? MinPlaytime { get; init; }

    /// <summary>Thumbnail image URL; null when not available.</summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>Board game title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Year published; null when not available.</summary>
    public int? YearPublished { get; init; }
}
