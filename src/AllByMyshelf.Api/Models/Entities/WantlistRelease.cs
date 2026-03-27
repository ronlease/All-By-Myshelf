namespace AllByMyshelf.Api.Models.Entities;

/// <summary>
/// Represents a release from the Discogs wantlist.
/// </summary>
public class WantlistRelease : CollectionEntityBase
{
    /// <summary>UTC timestamp of when this item was added to the wantlist; null when not provided by Discogs.</summary>
    public DateTimeOffset? AddedAt { get; set; }

    /// <summary>Artist names as returned by Discogs.</summary>
    public List<string> Artists { get; set; } = [];

    /// <summary>Full-size cover image URL as returned by Discogs; null when not provided.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>Discogs release ID used for upsert matching.</summary>
    public int DiscogsId { get; set; }

    /// <summary>Primary format description (e.g. "Vinyl", "CD").</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Primary genre; null when not populated by sync.</summary>
    public string? Genre { get; set; }

    /// <summary>Thumbnail image URL as returned by Discogs; null when not provided.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Release year; null when Discogs does not provide one.</summary>
    public int? Year { get; set; }
}
