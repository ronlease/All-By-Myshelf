namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Full detail representation of a single release, returned by the GET /api/v1/releases/{id} endpoint.
/// </summary>
public class ReleaseDetailDto
{
    /// <summary>List of artist names.</summary>
    public List<string> Artists { get; init; } = [];

    /// <summary>Discogs CDN URL for the full-resolution cover image. Null when Discogs does not provide a cover image.</summary>
    public string? CoverImageUrl { get; init; }

    /// <summary>Discogs release ID.</summary>
    public int DiscogsId { get; init; }

    /// <summary>Primary format description.</summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>Primary genre; null when not populated by sync.</summary>
    public string? Genre { get; init; }

    /// <summary>Highest marketplace price in USD; null when unavailable.</summary>
    public decimal? HighestPrice { get; init; }

    /// <summary>Application-generated identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Lowest marketplace price in USD; null when unavailable.</summary>
    public decimal? LowestPrice { get; init; }

    /// <summary>Median marketplace price in USD; null when unavailable.</summary>
    public decimal? MedianPrice { get; init; }

    /// <summary>User-provided listening notes; null when no notes have been entered.</summary>
    public string? Notes { get; init; }

    /// <summary>User-provided rating on a scale of 1-5; null when unrated.</summary>
    public int? Rating { get; init; }

    /// <summary>Release title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Release year; null when unknown.</summary>
    public int? Year { get; init; }
}
