namespace AllByMyshelf.Api.Models.Entities;

/// <summary>
/// Represents a vinyl release persisted from a Discogs collection sync.
/// </summary>
public class Release : CollectionEntityBase
{
    /// <summary>UTC timestamp of when this release was first added to the local database.</summary>
    public DateTimeOffset? AddedAt { get; set; }

    /// <summary>Artist names as returned by Discogs.</summary>
    public List<string> Artists { get; set; } = [];

    /// <summary>Full-size cover image URL as returned by Discogs; null when not provided.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>UTC timestamp of when detail/pricing data was last fetched from Discogs; null when not yet fetched.</summary>
    public DateTimeOffset? DetailSyncedAt { get; set; }

    /// <summary>Discogs release ID used for upsert matching.</summary>
    public int DiscogsId { get; set; }

    /// <summary>Primary format description (e.g. "Vinyl", "CD").</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Primary genre; null when not populated by sync.</summary>
    public string? Genre { get; set; }

    /// <summary>Highest marketplace price from Discogs; null when unavailable.</summary>
    public decimal? HighestPrice { get; set; }

    /// <summary>Lowest marketplace price from Discogs; null when unavailable.</summary>
    public decimal? LowestPrice { get; set; }

    /// <summary>Median marketplace price from Discogs; null when unavailable.</summary>
    public decimal? MedianPrice { get; set; }

    /// <summary>User-provided listening notes; up to 2000 characters.</summary>
    public string? Notes { get; set; }

    /// <summary>User-provided rating on a scale of 1-5; null when unrated.</summary>
    public int? Rating { get; set; }

    /// <summary>Thumbnail image URL as returned by Discogs; null when not provided.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Unique artist names extracted from the release tracklist; used for searching compilations.</summary>
    public List<string> TrackArtists { get; set; } = [];

    /// <summary>Full tracklist with position, title, and per-track artists.</summary>
    public ICollection<Track> Tracks { get; set; } = [];

    /// <summary>Release year; null when Discogs does not provide one.</summary>
    public int? Year { get; set; }
}
