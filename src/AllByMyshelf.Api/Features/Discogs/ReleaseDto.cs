namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Represents a single release in an API response.
/// </summary>
public class ReleaseDto
{
    /// <summary>List of artist names.</summary>
    public List<string> Artists { get; init; } = [];

    /// <summary>Primary format description.</summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>Primary genre; null when not populated by sync.</summary>
    public string? Genre { get; init; }

    /// <summary>Database identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Release title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Discogs CDN URL for the release thumbnail (~150x150 px). Null when Discogs does not provide a cover image.</summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>Unique artist names from the release tracklist; used for searching compilations.</summary>
    public List<string> TrackArtists { get; init; } = [];

    /// <summary>Release year; null when unknown.</summary>
    public int? Year { get; init; }
}
