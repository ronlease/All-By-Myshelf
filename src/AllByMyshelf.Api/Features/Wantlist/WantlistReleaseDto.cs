namespace AllByMyshelf.Api.Features.Wantlist;

/// <summary>
/// Public representation of a wantlist release.
/// </summary>
public record WantlistReleaseDto
{
    /// <summary>List of artist names.</summary>
    public required List<string> Artists { get; init; }

    /// <summary>Cover image URL; null when not available.</summary>
    public string? CoverImageUrl { get; init; }

    /// <summary>Discogs release ID.</summary>
    public required int DiscogsId { get; init; }

    /// <summary>Primary format description.</summary>
    public required string Format { get; init; }

    /// <summary>Primary genre; null when not available.</summary>
    public string? Genre { get; init; }

    /// <summary>Application-generated identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>Thumbnail image URL; null when not available.</summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>Release title.</summary>
    public required string Title { get; init; }

    /// <summary>Release year; null when not available.</summary>
    public int? Year { get; init; }
}
