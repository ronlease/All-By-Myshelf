namespace AllByMyshelf.Api.Models.Entities;

/// <summary>
/// Represents a board game persisted from a BoardGameGeek collection sync.
/// </summary>
public class BoardGame : CollectionEntityBase
{
    /// <summary>BoardGameGeek game ID used for upsert matching.</summary>
    public int BoardGameGeekId { get; set; }

    /// <summary>Full cover image URL as returned by BoardGameGeek; null when not provided.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>Game description; null when not available.</summary>
    public string? Description { get; set; }

    /// <summary>Designer names; empty when not available.</summary>
    public List<string> Designers { get; set; } = [];

    /// <summary>Primary category/genre; null when not available.</summary>
    public string? Genre { get; set; }

    /// <summary>Maximum number of players; null when not available.</summary>
    public int? MaxPlayers { get; set; }

    /// <summary>Maximum playtime in minutes; null when not available.</summary>
    public int? MaxPlaytime { get; set; }

    /// <summary>Minimum number of players; null when not available.</summary>
    public int? MinPlayers { get; set; }

    /// <summary>Minimum playtime in minutes; null when not available.</summary>
    public int? MinPlaytime { get; set; }

    /// <summary>Thumbnail image URL; null when not available.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Year published; null when not available.</summary>
    public int? YearPublished { get; set; }
}
