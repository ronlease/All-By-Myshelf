namespace AllByMyshelf.Api.Features.Discogs;

/// <summary>
/// Represents a release with incomplete data that may need manual attention.
/// </summary>
public record MaintenanceReleaseDto
{
    /// <summary>Artist name.</summary>
    public required string Artist { get; init; }

    /// <summary>Discogs release ID.</summary>
    public required int DiscogsId { get; init; }

    /// <summary>Application-generated identifier.</summary>
    public required Guid Id { get; init; }

    /// <summary>List of field names that are missing or incomplete.</summary>
    public required List<string> MissingFields { get; init; }

    /// <summary>Thumbnail image URL; null when not available.</summary>
    public string? ThumbnailUrl { get; init; }

    /// <summary>Release title.</summary>
    public required string Title { get; init; }
}
